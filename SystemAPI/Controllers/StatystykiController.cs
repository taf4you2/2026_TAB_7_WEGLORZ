using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models.DTOs.Admin;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/statystyki")]
public class StatystykiController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/statystyki/dzisiaj
    // Dane do dashboardu kasjera: sprzedaż dziś, aktywne karnety, przychód zmiany, oczekujące zwroty.
    [HttpGet("dzisiaj")]
    public async Task<IActionResult> GetToday()
    {
        var today = SkiResortClock.Today;
        var tomorrow = today.AddDays(1);
        var now = SkiResortClock.Now;

        // Transakcje z dzisiaj (sprzedaż biletów i karnetów — kwoty dodatnie)
        var todayTransactions = await db.Transactions
            .Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow)
            .ToListAsync();

        var ticketsSoldToday = todayTransactions.Count(t => t.Amount > 0);
        var shiftRevenue = todayTransactions.Sum(t => t.Amount);

        // Aktywne karnety w systemie
        var activePasses = await db.SkiPasses
            .Include(sp => sp.Status)
            .CountAsync(sp => sp.ValidFrom <= now
                && sp.ValidTo >= now
                && sp.Status != null
                && sp.Status.Name == "aktywny");

        // Oczekujące zwroty: karnety ze statusem "oczekuje_na_zwrot"
        // TODO: ustalić właściwą nazwę statusu w DictPassStatus
        var pendingReturns = await db.SkiPasses
            .Include(sp => sp.Status)
            .CountAsync(sp => sp.Status != null && sp.Status.Name == "oczekuje_na_zwrot");

        return Ok(new DashboardDto(ticketsSoldToday, activePasses, shiftRevenue, pendingReturns));
    }

    // GET /api/statystyki/infrastruktura
    // Zwraca status wyciągów i bramek
    [Authorize(Roles = "admin")]
    [HttpGet("infrastruktura")]
    public async Task<IActionResult> GetInfrastructureStatus()
    {
        var lifts = await db.Lifts
            .Include(l => l.Gates)
            .OrderBy(l => l.Name)
            .Select(l => new
            {
                l.Id,
                l.Name,
                IsActive = l.IsActive ?? true,
                Gates = l.Gates.Select(g => new { g.Id, g.Name, IsActive = g.IsActive ?? false }).ToList()
            })
            .ToListAsync();

        return Ok(lifts);
    }

    // GET /api/statystyki/nieudane-odbicia
    // Zwraca ostatnie 20 nieudanych prób przejścia przez bramkę
    [Authorize(Roles = "admin")]
    [HttpGet("nieudane-odbicia")]
    public async Task<IActionResult> GetFailedScans()
    {
        var failedScans = await db.GateScans
            .Include(gs => gs.Gate)
            .Include(gs => gs.VerificationResult)
            .Where(gs => gs.VerificationResult != null && gs.VerificationResult.Name != "ok")
            .OrderByDescending(gs => gs.ScanTime)
            .Take(20)
            .Select(gs => new
            {
                gs.Id,
                gs.CardId,
                GateName = gs.Gate != null ? gs.Gate.Name : "Nieznana",
                gs.ScanTime,
                Result = gs.VerificationResult != null ? gs.VerificationResult.Name : "Błąd"
            })
            .ToListAsync();

        return Ok(failedScans);
    }

    // GET /api/statystyki/sprzedaz-porownanie
    // Porównanie sprzedaży online vs stacjonarnej z ostatnich 30 dni
    [Authorize(Roles = "admin")]
    [HttpGet("sprzedaz-porownanie")]
    public async Task<IActionResult> GetSalesComparison()
    {
        var startDate = DateTime.UtcNow.AddDays(-30);

        var transactions = await db.Transactions
            .Where(t => t.TransactionDate >= startDate && t.Amount > 0)
            .ToListAsync();

        var onsiteSales = transactions.Where(t => t.CashierId != null).Sum(t => t.Amount);
        var onlineSales = transactions.Where(t => t.CashierId == null).Sum(t => t.Amount);

        // Liczba rezerwacji online vs stacjonarnych
        var onsiteCount = transactions.Count(t => t.CashierId != null);
        var onlineCount = transactions.Count(t => t.CashierId == null);

        return Ok(new
        {
            Onsite = new { Amount = onsiteSales, Count = onsiteCount },
            Online = new { Amount = onlineSales, Count = onlineCount }
        });
    }

    // GET /api/statystyki/aktywni-kasjerzy
    // Zwraca listę kasjerów, którzy mają otwartą zmianę (brak EndTime w ostatnim raporcie)
    [Authorize(Roles = "admin")]
    [HttpGet("aktywni-kasjerzy")]
    public async Task<IActionResult> GetActiveCashiers()
    {
        var activeShifts = await db.ShiftReports
            .Include(r => r.Cashier)
            .Where(r => r.EndTime == null)
            .Select(r => new
            {
                r.CashierId,
                Login = r.Cashier != null ? r.Cashier.Login : "Nieznany",
                r.StartTime
            })
            .ToListAsync();

        return Ok(activeShifts);
    }

    // GET /api/statystyki/oblozenie-minuty
    // Zwraca liczbę przejść przez bramki dla każdego wyciągu z ostatnich 15 minut
    [Authorize(Roles = "admin")]
    [HttpGet("oblozenie-minuty")]
    public async Task<IActionResult> GetRealtimeOccupancy()
    {
        var minutesAgo = SkiResortClock.Now.AddMinutes(-15);

        var occupancy = await db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Where(gs => gs.ScanTime >= minutesAgo && gs.VerificationResult != null && gs.VerificationResult.Name == "ok")
            .GroupBy(gs => new { gs.Gate!.LiftId, LiftName = gs.Gate!.Lift!.Name })
            .Select(g => new
            {
                g.Key.LiftId,
                g.Key.LiftName,
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(occupancy);
    }

    // GET /api/statystyki/ruch-godzinowy
    // Zwraca liczbe przejsc przez bramki w rozbiciu na godziny (dzisiaj)
    [Authorize(Roles = "admin")]
    [HttpGet("ruch-godzinowy")]
    public async Task<IActionResult> GetHourlyTraffic()
    {
        var today = SkiResortClock.Today;

        var traffic = await db.GateScans
            .Where(gs => gs.ScanTime >= today && gs.VerificationResult != null && gs.VerificationResult.Name == "ok")
            .GroupBy(gs => gs.ScanTime!.Value.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Hour)
            .ToListAsync();

        return Ok(traffic);
    }

    // GET /api/statystyki/statusy-kart
    // Zwraca liczbe kart w kazdym statusie
    [Authorize(Roles = "admin")]
    [HttpGet("statusy-kart")]
    public async Task<IActionResult> GetCardStatusDistribution()
    {
        var stats = await db.Cards
            .Include(c => c.Status)
            .GroupBy(c => c.Status!.Name)
            .Select(g => new
            {
                Status = g.Key ?? "Nieznany",
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(stats);
    }

    [HttpGet("wyciagi")]
    public async Task<IActionResult> GetLiftsTraffic()
    {
        var today = SkiResortClock.Today;
        var tomorrow = today.AddDays(1);

        var scansPerLift = await db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Where(gs => gs.ScanTime >= today && gs.ScanTime < tomorrow)
            .GroupBy(gs => gs.Gate!.Lift!.Name)
            .Select(g => new
            {
                LiftName = g.Key ?? "Nieznany",
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(scansPerLift);
    }

    [HttpGet("obciazenie-godzinowe")]
    public async Task<IActionResult> GetHourlyTraffic([FromQuery] DateTime? date)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = targetDate.AddDays(1);

        var scans = await db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Where(gs => gs.ScanTime >= targetDate && gs.ScanTime < nextDate && gs.ScanTime != null)
            .GroupBy(gs => new { LiftName = gs.Gate!.Lift!.Name, Hour = gs.ScanTime!.Value.Hour })
            .Select(g => new
            {
                LiftName = g.Key.LiftName ?? "Nieznany",
                Hour = g.Key.Hour,
                Count = g.Count()
            })
            .ToListAsync();

        var grouped = scans.GroupBy(s => s.LiftName).Select(g => new {
            LiftName = g.Key,
            Data = Enumerable.Range(0, 24).Select(h => g.FirstOrDefault(x => x.Hour == h)?.Count ?? 0).ToArray()
        });

        return Ok(grouped);
    }

    [HttpGet("przychody-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] string period = "day")
    {
        var now = DateTime.UtcNow;
        DateTime fromDate = now.Date.AddDays(-30); // Domyślnie ostatnie 30 dni
        
        if (period == "week") fromDate = now.Date.AddDays(-7*12);
        else if (period == "month") fromDate = now.Date.AddMonths(-12);

        var transactions = await db.Transactions
            .Where(t => t.TransactionDate >= fromDate && t.Amount > 0)
            .ToListAsync();

        var grouped = transactions
            .Where(t => t.TransactionDate.HasValue)
            .GroupBy(t => {
                var d = t.TransactionDate!.Value;
                if (period == "month") return new DateTime(d.Year, d.Month, 1).ToString("yyyy-MM");
                if (period == "week") return d.Date.AddDays(-(int)d.DayOfWeek).ToString("yyyy-MM-dd");
                return d.ToString("yyyy-MM-dd");
            })
            .OrderBy(g => g.Key)
            .Select(g => new {
                DateLabel = g.Key,
                Revenue = g.Sum(x => x.Amount)
            });

        return Ok(grouped);
    }

    [HttpGet("kanaly-sprzedazy")]
    public async Task<IActionResult> GetSalesChannels()
    {
        var nowUtc = DateTime.UtcNow;
        var startOfMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var transactions = await db.Transactions
            .Where(t => t.TransactionDate >= startOfMonth && t.Amount > 0)
            .ToListAsync();

        var posSales = transactions.Where(t => t.CashierId != null).Sum(t => t.Amount);
        var onlineSales = transactions.Where(t => t.ReservationId != null && t.CashierId == null).Sum(t => t.Amount);

        return Ok(new { PosSales = posSales, OnlineSales = onlineSales });
    }

    [HttpGet("popularnosc-karnetow")]
    public async Task<IActionResult> GetPassPopularity()
    {
        var nowUtc = DateTime.UtcNow;
        var startOfMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var passes = await db.SkiPasses
            .Include(sp => sp.Tariff)
            .Where(sp => sp.ValidFrom >= startOfMonth || sp.ValidTo >= startOfMonth)
            .GroupBy(sp => sp.Tariff!.Name)
            .Select(g => new {
                TariffName = g.Key ?? "Inny",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        return Ok(passes);
    }

    // GET /api/statystyki/activity-feed
    // Zwraca listę ostatnich skanowań kart (dla podglądu "Live Activity Feed")
    [Authorize(Roles = "admin")]
    [HttpGet("activity-feed")]
    public async Task<IActionResult> GetActivityFeed([FromQuery] int limit = 50)
    {
        var feed = await db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Include(gs => gs.VerificationResult)
            .OrderByDescending(gs => gs.ScanTime)
            .Take(limit)
            .Select(gs => new ActivityFeedDto
            {
                Id = gs.Id,
                CardRfid = gs.CardId ?? "Nieznana",
                Location = (gs.Gate != null ? gs.Gate.Name : "") + (gs.Gate != null && gs.Gate.Lift != null ? " - " + gs.Gate.Lift.Name : ""),
                Timestamp = gs.ScanTime ?? SkiResortClock.Now,
                Status = gs.VerificationResult != null ? gs.VerificationResult.Name : "Brak statusu",
                Reason = gs.VerificationResult != null && gs.VerificationResult.Name != "ok" ? "Odmowa dostępu" : ""
            })
            .ToListAsync();

        return Ok(feed);
    }

    // GET /api/statystyki/sales-chart
    // Zwraca dane do wykresu sprzedaży w rozbiciu na dni
    [Authorize(Roles = "admin")]
    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChart([FromQuery] int days = 7)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);

        var transactions = await db.Transactions
            .Where(t => t.TransactionDate >= startDate && t.Amount > 0)
            .ToListAsync();

        var dto = new SalesChartDto();

        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            dto.Labels.Add(date.ToString("yyyy-MM-dd"));

            var dailyTrans = transactions.Where(t => t.TransactionDate >= date && t.TransactionDate < date.AddDays(1)).ToList();
            dto.OnsiteValues.Add(dailyTrans.Where(t => t.CashierId != null).Sum(t => t.Amount));
            dto.OnlineValues.Add(dailyTrans.Where(t => t.CashierId == null).Sum(t => t.Amount));
        }

        return Ok(dto);
    }

    // GET /api/statystyki/sales-structure
    // Zwraca udzial procentowy taryf w przychodach
    [Authorize(Roles = "admin")]
    [HttpGet("sales-structure")]
    public async Task<IActionResult> GetSalesStructure([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        var passes = await db.SkiPasses
            .Include(sp => sp.Tariff)
            .Where(sp => sp.ValidFrom >= startDate && sp.Tariff != null)
            .GroupBy(sp => sp.Tariff!.Name)
            .Select(g => new
            {
                Name = g.Key,
                Value = g.Sum(x => x.Tariff!.Price)
            })
            .ToListAsync();

        var dto = new SalesStructureDto();
        foreach (var p in passes)
        {
            dto.Labels.Add(p.Name ?? "Nieznana");
            dto.Values.Add(p.Value ?? 0m);
        }

        return Ok(dto);
    }
}

public record DashboardDto(
    int TicketsSoldToday,
    int ActivePasses,
    decimal ShiftRevenue,
    int PendingReturns
);

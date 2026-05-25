using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

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
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var nowUtc = DateTime.UtcNow;

        // Transakcje z dzisiaj (sprzedaż biletów i karnetów — kwoty dodatnie)
        var todayTransactions = await db.Transactions
            .Where(t => t.TransactionDate >= todayUtc && t.TransactionDate < tomorrowUtc)
            .ToListAsync();

        var ticketsSoldToday = todayTransactions.Count(t => t.Amount > 0);
        var shiftRevenue = todayTransactions.Sum(t => t.Amount);

        // Aktywne karnety w systemie
        var activePasses = await db.SkiPasses
            .CountAsync(sp => sp.ValidFrom <= nowUtc && sp.ValidTo >= nowUtc);

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
        var minutesAgo = DateTime.UtcNow.AddMinutes(-15);

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
        var today = DateTime.UtcNow.Date;

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
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var scansPerLift = await db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Where(gs => gs.ScanTime >= todayUtc && gs.ScanTime < tomorrowUtc)
            .GroupBy(gs => gs.Gate!.Lift!.Name)
            .Select(g => new
            {
                LiftName = g.Key ?? "Nieznany",
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(scansPerLift);
    }
}

public record DashboardDto(
    int TicketsSoldToday,
    int ActivePasses,
    decimal ShiftRevenue,
    int PendingReturns
);

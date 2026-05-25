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
}

public record DashboardDto(
    int TicketsSoldToday,
    int ActivePasses,
    decimal ShiftRevenue,
    int PendingReturns
);

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

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
}

public record DashboardDto(
    int TicketsSoldToday,
    int ActivePasses,
    decimal ShiftRevenue,
    int PendingReturns
);

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/raport-zmiany")]
public class RaportZmianyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/raport-zmiany?cashierId=1
    // Agreguje transakcje bieżącej zmiany dla danego kasjera.
    [HttpGet]
    public async Task<IActionResult> GetShiftReport([FromQuery] int cashierId)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var transactions = await db.Transactions
            .Include(t => t.OperationType)
            .Where(t => t.CashierId == cashierId
                     && t.TransactionDate >= todayUtc
                     && t.TransactionDate < tomorrowUtc)
            .ToListAsync();

        // TODO: rozróżnić typy operacji na podstawie DictOperationType.Name
        // Tymczasowo: dodatnie = sprzedaż, ujemne = zwroty
        var sales = transactions.Where(t => t.Amount > 0).ToList();
        var returns = transactions.Where(t => t.Amount < 0).ToList();

        // TODO: rozróżnić gotówkę od karty — brak pola w modelu Transaction;
        // do zaimplementowania po dodaniu pola "payment_method" do Transaction

        var cashier = await db.Cashiers.FindAsync(cashierId);

        return Ok(new ShiftReportDto(
            CashierLogin: cashier?.Login ?? $"ID {cashierId}",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            TotalSalesCount: sales.Count,
            TotalSalesAmount: sales.Sum(t => t.Amount),
            TotalReturnsCount: Math.Abs(returns.Count),
            TotalReturnsAmount: returns.Sum(t => t.Amount),
            NetRevenue: transactions.Sum(t => t.Amount),
            CashAmount: 0,        // TODO
            CardAmount: 0         // TODO
        ));
    }

    // POST /api/raport-zmiany/zamknij?cashierId=1
    // Zamknięcie zmiany — zapisuje ShiftReport i oznacza zmianę jako zakończoną.
    [HttpPost("zamknij")]
    public async Task<IActionResult> CloseShift([FromQuery] int cashierId)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var transactions = await db.Transactions
            .Where(t => t.CashierId == cashierId
                     && t.TransactionDate >= todayUtc
                     && t.TransactionDate < tomorrowUtc)
            .ToListAsync();

        // TODO: sprawdzić czy zmiana nie jest już zamknięta
        var existingReport = await db.ShiftReports
            .FirstOrDefaultAsync(r => r.CashierId == cashierId && r.EndTime == null);

        if (existingReport != null)
        {
            existingReport.EndTime = DateTime.UtcNow;
            existingReport.TotalRevenue = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            existingReport.TotalDepositReturns = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
        }
        else
        {
            db.ShiftReports.Add(new ShiftReport
            {
                CashierId = cashierId,
                StartTime = todayUtc,
                EndTime = DateTime.UtcNow,
                TotalRevenue = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalDepositReturns = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount))
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }
}

record ShiftReportDto(
    string CashierLogin,
    DateOnly Date,
    int TotalSalesCount,
    decimal TotalSalesAmount,
    int TotalReturnsCount,
    decimal TotalReturnsAmount,
    decimal NetRevenue,
    decimal CashAmount,
    decimal CardAmount
);

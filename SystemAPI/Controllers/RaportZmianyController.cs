using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/raport-zmiany")]
public class RaportZmianyController(SkiResortDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetShiftReport()
    {
        var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var shiftStart = await GetCurrentShiftStart(cashierId);
        var now = DateTime.UtcNow;

        var transactions = await db.Transactions
            .Include(t => t.OperationType)
            .Where(t => t.CashierId == cashierId
                     && t.TransactionDate >= shiftStart
                     && t.TransactionDate < now)
            .ToListAsync();

        var sales = transactions.Where(IsSale).ToList();
        var returns = transactions.Where(IsReturn).ToList();
        var salesAmount = sales.Sum(t => Math.Abs(t.Amount));
        var returnsAmount = returns.Sum(t => Math.Abs(t.Amount));

        var cashier = await db.Cashiers.FindAsync(cashierId);

        return Ok(new ShiftReportDto(
            CashierLogin: cashier?.Login ?? $"ID {cashierId}",
            Date: DateOnly.FromDateTime(shiftStart),
            TotalSalesCount: sales.Count,
            TotalSalesAmount: salesAmount,
            TotalReturnsCount: returns.Count,
            TotalReturnsAmount: returnsAmount,
            NetRevenue: salesAmount - returnsAmount,
            CashAmount: 0,
            CardAmount: 0
        ));
    }

    [HttpPost("zamknij")]
    public async Task<IActionResult> CloseShift()
    {
        var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var shiftStart = await GetCurrentShiftStart(cashierId);
        var shiftEnd = DateTime.UtcNow;

        var transactions = await db.Transactions
            .Include(t => t.OperationType)
            .Where(t => t.CashierId == cashierId
                     && t.TransactionDate >= shiftStart
                     && t.TransactionDate < shiftEnd)
            .ToListAsync();

        var salesAmount = transactions.Where(IsSale).Sum(t => Math.Abs(t.Amount));
        var returnsAmount = transactions.Where(IsReturn).Sum(t => Math.Abs(t.Amount));

        var existingReport = await db.ShiftReports
            .FirstOrDefaultAsync(r => r.CashierId == cashierId && r.EndTime == null);

        if (existingReport != null)
        {
            existingReport.EndTime = DateTime.UtcNow;
            existingReport.TotalRevenue = salesAmount - returnsAmount;
            existingReport.TotalDepositReturns = returnsAmount;
        }
        else
        {
            db.ShiftReports.Add(new ShiftReport
            {
                CashierId = cashierId,
                StartTime = shiftStart,
                EndTime = shiftEnd,
                TotalRevenue = salesAmount - returnsAmount,
                TotalDepositReturns = returnsAmount
            });
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<DateTime> GetCurrentShiftStart(int cashierId)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var lastClosedAt = await db.ShiftReports
            .Where(r => r.CashierId == cashierId && r.EndTime != null)
            .MaxAsync(r => (DateTime?)r.EndTime);

        return lastClosedAt.HasValue && lastClosedAt.Value > todayUtc
            ? DateTime.SpecifyKind(lastClosedAt.Value, DateTimeKind.Utc)
            : todayUtc;
    }

    private static bool IsSale(Transaction transaction) =>
        transaction.OperationType?.Name?.StartsWith("sprzedaz_", StringComparison.OrdinalIgnoreCase) == true
        || transaction.Amount > 0;

    private static bool IsReturn(Transaction transaction) =>
        transaction.OperationType?.Name?.StartsWith("zwrot_", StringComparison.OrdinalIgnoreCase) == true
        || transaction.Amount < 0;
}

public record ShiftReportDto(
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

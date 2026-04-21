using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/transakcje")]
public class TransakcjeController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/transakcje?date=2026-04-15&cashierId=1
    // Zwraca historię transakcji z opcjonalnym filtrem po dacie i kasjerze.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateOnly? date,
        [FromQuery] int? cashierId)
    {
        var query = db.Transactions
            .Include(t => t.OperationType)
            .Include(t => t.Cashier)
            .Include(t => t.Reservation)
                .ThenInclude(r => r!.SkiPasses)
                    .ThenInclude(sp => sp.Tariff)
            .AsQueryable();

        if (date.HasValue)
        {
            var from = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = from.AddDays(1);
            query = query.Where(t => t.TransactionDate >= from && t.TransactionDate < to);
        }

        if (cashierId.HasValue)
            query = query.Where(t => t.CashierId == cashierId);

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        var result = transactions.Select(t =>
        {
            var firstPass = t.Reservation?.SkiPasses.FirstOrDefault();
            return new TransactionDto(
                t.Id,
                t.OperationType?.Name,
                firstPass?.Tariff?.Name,
                t.Amount,
                t.TransactionDate,
                t.Cashier?.Login
            );
        });

        return Ok(result);
    }
}

public record TransactionDto(
    int Id,
    string? OperationType,
    string? Tariff,
    decimal Amount,
    DateTime? Date,
    string? CashierLogin
);

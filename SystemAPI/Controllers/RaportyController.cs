using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/raporty")]
public class RaportyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/raporty/przejazdy?cardId={rfid}&date={YYYY-MM-DD}
    // Historia skanów bramki dla danej karty.
    // date — opcjonalne; bez niego zwraca wszystkie przejazdy.
    [HttpGet("przejazdy")]
    public async Task<IActionResult> GetRideHistory(
        [FromQuery] string cardId,
        [FromQuery] DateOnly? date)
    {
        if (string.IsNullOrEmpty(cardId))
            return BadRequest(new { message = "Parametr cardId jest wymagany." });

        var query = db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Include(gs => gs.VerificationResult)
            .Where(gs => gs.CardId == cardId)
            .AsQueryable();

        if (date.HasValue)
        {
            var from = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = from.AddDays(1);
            query = query.Where(gs => gs.ScanTime >= from && gs.ScanTime < to);
        }

        var scans = await query
            .OrderByDescending(gs => gs.ScanTime)
            .ToListAsync();

        var result = scans.Select(gs => new GateScanDto(
            gs.Id,
            gs.CardId,
            gs.Gate?.Name,
            gs.Gate?.Lift?.Name,
            gs.ScanTime,
            gs.VerificationResult?.Name
        ));

        return Ok(result);
    }

    // 1. Raport zmianowy (Kasa)
    [Authorize(Roles = "admin")]
    [HttpGet("zmiany")]
    public async Task<IActionResult> GetShiftReports()
    {
        var reports = await db.ShiftReports
            .Include(r => r.Cashier)
            .OrderByDescending(r => r.StartTime)
            .Select(r => new
            {
                r.Id,
                CashierLogin = r.Cashier != null ? r.Cashier.Login : "Nieznany",
                r.StartTime,
                r.EndTime,
                r.TotalRevenue,
                r.TotalDepositReturns,
                r.CardsIssuedCount
            })
            .ToListAsync();

        return Ok(reports);
    }

    // 2. Raport sprzedaży ogólnej
    [Authorize(Roles = "admin")]
    [HttpGet("sprzedaz-ogolna")]
    public async Task<IActionResult> GetGeneralSalesReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var adminId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
        
        // Logowanie wygenerowania raportu
        var reportType = await db.DictReportTypes.FirstOrDefaultAsync(t => t.Name == "sprzedaz_ogolna");
        db.AdminReports.Add(new SystemStacjiNarciarskiejDLL.Models.AdminReport
        {
            AdminId = adminId,
            ReportTypeId = reportType?.Id,
            GeneratedAt = DateTime.UtcNow,
            ReportParameters = $"from={from:yyyy-MM-dd};to={to:yyyy-MM-dd}"
        });
        await db.SaveChangesAsync();

        var transactions = await db.Transactions
            .Include(t => t.OperationType)
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .ToListAsync();

        var onsite = transactions.Where(t => t.CashierId != null);
        var online = transactions.Where(t => t.CashierId == null);

        var result = new
        {
            TotalRevenue = transactions.Sum(t => t.Amount),
            Onsite = new
            {
                Amount = onsite.Sum(t => t.Amount),
                Count = onsite.Count(),
                ByOperation = onsite.GroupBy(t => t.OperationType?.Name ?? "Inne")
                                    .Select(g => new { Operation = g.Key, Amount = g.Sum(x => x.Amount) })
            },
            Online = new
            {
                Amount = online.Sum(t => t.Amount),
                Count = online.Count()
            },
            GeneratedAt = DateTime.UtcNow
        };

        return Ok(result);
    }

    // 3. Raport przepustowości wyciągów
    [Authorize(Roles = "admin")]
    [HttpGet("przepustowosc-wyciagow")]
    public async Task<IActionResult> GetLiftThroughput([FromQuery] DateOnly date)
    {
        var from = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = from.AddDays(1);

        var scans = await db.GateScans
            .Include(gs => gs.Gate)
                .ThenInclude(g => g!.Lift)
            .Include(gs => gs.VerificationResult)
            .Where(gs => gs.ScanTime >= from && gs.ScanTime < to 
                     && gs.VerificationResult != null && gs.VerificationResult.Name == "ok")
            .ToListAsync();

        var throughput = scans
            .GroupBy(gs => new { gs.Gate!.LiftId, LiftName = gs.Gate!.Lift!.Name })
            .Select(g => new
            {
                g.Key.LiftId,
                g.Key.LiftName,
                HourlyStats = g.GroupBy(gs => gs.ScanTime!.Value.Hour)
                               .Select(hg => new { Hour = hg.Key, Count = hg.Count() })
                               .OrderBy(x => x.Hour)
            })
            .ToList();

        return Ok(throughput);
    }

    // 5. Administracyjne zamykanie zmiany kasjera
    [Authorize(Roles = "admin")]
    [HttpPost("zamknij-kasjera/{cashierId}")]
    public async Task<IActionResult> AdminCloseShift(int cashierId)
    {
        var shift = await db.ShiftReports
            .FirstOrDefaultAsync(r => r.CashierId == cashierId && r.EndTime == null);

        if (shift == null)
            return BadRequest(new { message = "Kasjer nie ma otwartej zmiany." });

        var shiftStart = shift.StartTime ?? DateTime.UtcNow.Date;
        var shiftEnd = DateTime.UtcNow;

        // Podliczanie transakcji
        var transactions = await db.Transactions
            .Where(t => t.CashierId == cashierId && t.TransactionDate >= shiftStart && t.TransactionDate <= shiftEnd)
            .ToListAsync();

        var revenue = transactions.Sum(t => t.Amount);
        var returns = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));

        shift.EndTime = shiftEnd;
        shift.TotalRevenue = revenue;
        shift.TotalDepositReturns = returns;

        await db.SaveChangesAsync();
        return Ok(new { message = "Zmiana zostala pomyslnie zamknieta przez administratora.", revenue });
    }
}


public record GateScanDto(
    int Id,
    string? CardId,
    string? GateName,
    string? LiftName,
    DateTime? ScanTime,
    string? Result
);

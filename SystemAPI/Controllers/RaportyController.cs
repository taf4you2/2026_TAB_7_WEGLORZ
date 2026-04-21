using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

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
}

public record GateScanDto(
    int Id,
    string? CardId,
    string? GateName,
    string? LiftName,
    DateTime? ScanTime,
    string? Result
);

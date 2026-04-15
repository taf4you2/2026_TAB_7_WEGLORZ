using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/karty")]
public class KartyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/karty?status=aktywna&search=A3:F2
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? search)
    {
        var query = db.Cards
            .Include(c => c.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status != null && c.Status.Name == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Id.Contains(search));

        var cards = await query.ToListAsync();

        var today = DateTime.UtcNow;
        var result = cards.Select(c =>
        {
            var activePass = c.SkiPasses
                .FirstOrDefault(sp => sp.ValidFrom <= today && sp.ValidTo >= today);
            return new CardDto(
                c.Id,
                c.Status?.Name ?? "nieznany",
                null, // TODO: właściciel przez SkiPass → Reservation → User.Email (brak imienia w modelu)
                activePass?.Tariff?.Name,
                activePass?.ValidTo,
                true  // TODO: pobrać info o kaucji z transakcji
            );
        });

        return Ok(result);
    }

    // GET /api/karty/{rfid}
    [HttpGet("{rfid}")]
    public async Task<IActionResult> GetByRfid(string rfid)
    {
        var card = await db.Cards
            .Include(c => c.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Reservation)
                    .ThenInclude(r => r!.User)
            .FirstOrDefaultAsync(c => c.Id == rfid);

        if (card == null)
            return NotFound(new { message = $"Karta {rfid} nie istnieje w systemie." });

        var today = DateTime.UtcNow;
        var activePass = card.SkiPasses
            .FirstOrDefault(sp => sp.ValidFrom <= today && sp.ValidTo >= today);

        return Ok(new CardDto(
            card.Id,
            card.Status?.Name ?? "nieznany",
            activePass?.Reservation?.User?.Email,
            activePass?.Tariff?.Name,
            activePass?.ValidTo,
            true // TODO: kaucja
        ));
    }

    // POST /api/karty
    // Body: { "id": "AA:BB:CC:DD" }
    [HttpPost]
    public async Task<IActionResult> IssueCard([FromBody] IssueCardRequest req)
    {
        if (await db.Cards.AnyAsync(c => c.Id == req.Id))
            return Conflict(new { message = "Karta o tym identyfikatorze już istnieje." });

        // TODO: pobrać id statusu "wolna" z DictCardStatus
        var freeStatus = await db.DictCardStatuses.FirstOrDefaultAsync(s => s.Name == "wolna");

        var card = new Card
        {
            Id = req.Id,
            StatusId = freeStatus?.Id,
            AddedToPoolAt = DateTime.UtcNow
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync();

        // TODO: zarejestrować transakcję kaucji 20 zł

        return CreatedAtAction(nameof(GetByRfid), new { rfid = card.Id }, new { card.Id });
    }
}

record CardDto(
    string Id,
    string Status,
    string? Owner,
    string? ActivePassType,
    DateTime? ValidTo,
    bool DepositPaid
);

record IssueCardRequest(string Id);

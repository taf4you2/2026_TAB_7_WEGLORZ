using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[Authorize]
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
            .Include(c => c.User)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Reservation)
                    .ThenInclude(r => r!.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status != null && c.Status.Name == status);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Id.Contains(search));

        var cards = await query.ToListAsync();
        return Ok(cards.Select(ToCardDto));
    }

    // GET /api/karty/{rfid}/weryfikacja-wydania
    [HttpGet("{rfid}/weryfikacja-wydania")]
    public async Task<IActionResult> VerifyForIssue(string rfid)
    {
        var card = await LoadCard(rfid).FirstOrDefaultAsync();

        if (card == null)
            return Ok(new CardIssueVerificationDto(false, $"Karta {rfid} nie istnieje w systemie.", null));

        var activePass = GetActivePass(card);
        if (activePass != null)
        {
            var tariffName = activePass.Tariff?.Name ?? $"ID {activePass.Id}";
            return Ok(new CardIssueVerificationDto(false, $"Karta ma aktywny karnet: {tariffName}.", ToCardDto(card)));
        }

        if (!string.Equals(card.Status?.Name, "wolna", StringComparison.OrdinalIgnoreCase))
            return Ok(new CardIssueVerificationDto(false, $"Karta ma status '{card.Status?.Name ?? "nieznany"}'.", ToCardDto(card)));

        if (!string.IsNullOrWhiteSpace(card.PhysicalCondition) &&
            card.PhysicalCondition.Contains("uszk", StringComparison.OrdinalIgnoreCase))
            return Ok(new CardIssueVerificationDto(false, $"Karta ma stan fizyczny '{card.PhysicalCondition}'.", ToCardDto(card)));

        return Ok(new CardIssueVerificationDto(true, "Karta jest wolna i gotowa do wydania.", ToCardDto(card)));
    }

    // GET /api/karty/{rfid}
    [HttpGet("{rfid}")]
    public async Task<IActionResult> GetByRfid(string rfid)
    {
        var card = await LoadCard(rfid).FirstOrDefaultAsync();

        if (card == null)
            return NotFound(new { message = $"Karta {rfid} nie istnieje w systemie." });

        return Ok(ToCardDto(card));
    }

    // POST /api/karty
    [HttpPost]
    public async Task<IActionResult> IssueCard([FromBody] IssueCardRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Id))
            return BadRequest(new { message = "RFID karty jest wymagane." });

        if (await db.Cards.AnyAsync(c => c.Id == req.Id))
            return Conflict(new { message = "Karta o tym identyfikatorze juz istnieje." });

        var freeStatus = await db.DictCardStatuses.FirstOrDefaultAsync(s => s.Name == "wolna");

        var card = new Card
        {
            Id = req.Id.Trim(),
            StatusId = freeStatus?.Id,
            DepositPaid = false,
            PhysicalCondition = "dobry",
            AddedToPoolAt = DateTime.UtcNow
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByRfid), new { rfid = card.Id }, new { card.Id });
    }

    // DELETE /api/karty/{rfid}
    [HttpDelete("{rfid}")]
    public async Task<IActionResult> DeleteCard(string rfid)
    {
        var card = await db.Cards
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .Include(c => c.GateScans)
            .FirstOrDefaultAsync(c => c.Id == rfid);

        if (card == null)
            return NotFound(new { message = $"Karta {rfid} nie istnieje w systemie." });

        if (GetActivePass(card) != null)
            return Conflict(new { message = "Nie mozna usunac karty z aktywnym karnetem." });

        db.GateScans.RemoveRange(card.GateScans);
        foreach (var pass in card.SkiPasses)
            pass.CardId = null;

        db.Cards.Remove(card);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{rfid}/blokuj")]
    public async Task<IActionResult> BlockCard(string rfid, [FromBody] BlockCardRequest req)
    {
        var card = await db.Cards.FirstOrDefaultAsync(c => c.Id == rfid);
        if (card == null) return NotFound(new { message = $"Karta {rfid} nie istnieje." });

        var blockedId = await db.DictCardStatuses.Where(s => s.Name == "zastrzezony").Select(s => (int?)s.Id).FirstOrDefaultAsync();
        card.StatusId = blockedId;
        card.BlockReason = req.Reason;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{rfid}/odblokuj")]
    public async Task<IActionResult> UnblockCard(string rfid)
    {
        var card = await LoadCard(rfid).FirstOrDefaultAsync();
        if (card == null) return NotFound(new { message = $"Karta {rfid} nie istnieje." });

        var hasHeldPass = card.SkiPasses.Any(sp => sp.Status?.Name is "aktywny" or "zablokowany" or "zwrocony");
        var statusName = hasHeldPass ? "zajeta" : "wolna";
        var statusId = await db.DictCardStatuses.Where(s => s.Name == statusName).Select(s => (int?)s.Id).FirstOrDefaultAsync();
        card.StatusId = statusId;
        card.BlockReason = null;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{rfid}/zwrot")]
    public async Task<IActionResult> ReturnCard(string rfid)
    {
        var card = await LoadCard(rfid).FirstOrDefaultAsync();
        if (card == null) return NotFound(new { message = $"Karta {rfid} nie istnieje." });

        var hasBlockedPass = card.SkiPasses.Any(sp => sp.Status?.Name == "zablokowany");
        if (hasBlockedPass)
            return Conflict(new { message = "Nie mozna odebrac karty z zablokowanym karnetem. Najpierw odblokuj karnet." });

        // Aktywne karnety nie blokują odbioru — kasjer może odebrać kartę wczesniej.
        // Karnety zostają wygaszone (status "wygasly"), karta wraca do puli.
        var now = DateTime.UtcNow;
        var expiredStatusId = await db.DictPassStatuses
            .Where(s => s.Name == "wygasly")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        if (expiredStatusId.HasValue)
        {
            var activePasses = card.SkiPasses
                .Where(sp => sp.Status?.Name == "aktywny" && sp.ValidFrom <= now && sp.ValidTo >= now)
                .ToList();
            foreach (var pass in activePasses)
                pass.StatusId = expiredStatusId.Value;
        }

        var depositReturn = card.DepositPaid == true ? 20m : 0m;
        var freeId = await db.DictCardStatuses.Where(s => s.Name == "wolna").Select(s => (int?)s.Id).FirstOrDefaultAsync();
        card.StatusId = freeId;
        card.UserId = null;
        card.DepositPaid = false;
        card.BlockReason = null;

        if (depositReturn > 0)
        {
            var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "kaucja");
            db.Transactions.Add(new Transaction
            {
                CashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                OperationTypeId = opType?.Id,
                Amount = -depositReturn,
                TransactionDate = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { depositReturn });
    }

    private IQueryable<Card> LoadCard(string rfid) =>
        db.Cards
            .Include(c => c.Status)
            .Include(c => c.User)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Reservation)
                    .ThenInclude(r => r!.User)
            .Where(c => c.Id == rfid);

    private static SkiPass? GetActivePass(Card card)
    {
        var now = DateTime.UtcNow;
        return card.SkiPasses.FirstOrDefault(sp =>
            sp.ValidFrom <= now &&
            sp.ValidTo >= now &&
            string.Equals(sp.Status?.Name, "aktywny", StringComparison.OrdinalIgnoreCase));
    }

    private static CardDto ToCardDto(Card card)
    {
        var activePass = GetActivePass(card);
        return new CardDto(
            card.Id,
            card.Status?.Name ?? "nieznany",
            card.User?.Email ?? activePass?.Reservation?.User?.Email,
            activePass?.Tariff?.Name,
            activePass?.Id,
            activePass?.ValidTo,
            card.DepositPaid == true,
            card.BlockReason
        );
    }

    // POST /api/karty/partia
    [HttpPost("partia")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateCardBatch([FromBody] CardBatchRequest req)
    {
        var freeStatus = await db.DictCardStatuses.FirstOrDefaultAsync(s => s.Name == "wolna");
        var cardsToInsert = new List<Card>();

        for (int i = 0; i < req.Count; i++)
        {
            var cardId = $"{req.Prefix}{(req.StartNumber + i).ToString(new string('0', req.NumberLength))}";
            cardsToInsert.Add(new Card
            {
                Id = cardId,
                StatusId = freeStatus?.Id,
                DepositPaid = false,
                PhysicalCondition = "nowa",
                AddedToPoolAt = DateTime.UtcNow
            });
        }

        var generatedIds = cardsToInsert.Select(c => c.Id).ToList();
        var existing = await db.Cards.Where(c => generatedIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
        cardsToInsert.RemoveAll(c => existing.Contains(c.Id));

        if (cardsToInsert.Any())
        {
            db.Cards.AddRange(cardsToInsert);
            await db.SaveChangesAsync();
        }

        return Ok(new { GeneratedCount = cardsToInsert.Count, SkippedCount = req.Count - cardsToInsert.Count });
    }

    // PUT /api/karty/partia/zablokuj
    [HttpPut("partia/zablokuj")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> BlockCardBatch([FromBody] BlockBatchRequest req)
    {
        var blockedStatus = await db.DictCardStatuses.FirstOrDefaultAsync(s => s.Name == "zastrzezony");
        var cards = await db.Cards.Where(c => c.Id.StartsWith(req.Prefix)).ToListAsync();
        
        var toBlock = cards.Where(c => 
        {
            var numPart = c.Id.Substring(req.Prefix.Length);
            if (int.TryParse(numPart, out int num))
                return num >= req.StartNumber && num <= req.EndNumber;
            return false;
        }).ToList();

        foreach(var card in toBlock)
        {
            card.StatusId = blockedStatus?.Id;
            card.BlockReason = req.Reason;
        }

        await db.SaveChangesAsync();
        return Ok(new { BlockedCount = toBlock.Count });
    }
}

public record CardDto(
    string Id,
    string Status,
    string? Owner,
    string? ActivePassType,
    int? ActivePassId,
    DateTime? ValidTo,
    bool DepositPaid,
    string? BlockReason
);

public record IssueCardRequest(string Id);
public record BlockCardRequest(string Reason);
public record CardBatchRequest(string Prefix, int StartNumber, int NumberLength, int Count);
public record BlockBatchRequest(string Prefix, int StartNumber, int EndNumber, string Reason);

public record CardIssueVerificationDto(
    bool CanIssue,
    string Message,
    CardDto? Card
);

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/karnety")]
public class KarnetyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/karnety?cardId={rfid}  — lista karnetów dla danej karty
    [HttpGet]
    public async Task<IActionResult> GetByCard([FromQuery] string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return BadRequest(new { message = "Parametr cardId jest wymagany." });

        var passes = await db.SkiPasses
            .Include(sp => sp.Status)
            .Include(sp => sp.Tariff)
                .ThenInclude(t => t!.PassType)
            .Include(sp => sp.Card)
            .Include(sp => sp.Reservation)
                .ThenInclude(r => r!.User)
            .Where(sp => sp.CardId == cardId)
            .OrderByDescending(sp => sp.ValidFrom)
            .ToListAsync();

        return Ok(passes.Select(ToDto));
    }

    // GET /api/karnety/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var pass = await db.SkiPasses
            .Include(sp => sp.Status)
            .Include(sp => sp.Tariff)
                .ThenInclude(t => t!.PassType)
            .Include(sp => sp.Card)
            .Include(sp => sp.Reservation)
                .ThenInclude(r => r!.User)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (pass == null) return NotFound();

        return Ok(ToDto(pass));
    }

    // POST /api/karnety  — UC2: Sprzedaj karnet
    [HttpPost]
    public async Task<IActionResult> CreatePass([FromBody] CreatePassRequest req)
    {
        var card = await db.Cards.FindAsync(req.CardId);
        if (card == null)
            return BadRequest(new { message = $"Karta {req.CardId} nie istnieje." });

        var tariff = await db.Tariffs.FindAsync(req.TariffId);
        if (tariff == null)
            return BadRequest(new { message = "Taryfa nie istnieje." });

        // TODO: pobrać właściwy status "aktywny" z DictPassStatus
        var activeStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "aktywny");
        // TODO: pobrać właściwy status rezerwacji z DictReservationStatus
        var reservationStatus = await db.DictReservationStatuses.FirstOrDefaultAsync(s => s.Name == "potwierdzona");
        // TODO: pobrać typ operacji "sprzedaż karnetu" z DictOperationType
        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "sprzedaz_karnetu");

        var reservation = new Reservation
        {
            ReservationNumber = $"RES-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            ReservationDate = DateTime.UtcNow,
            StatusId = reservationStatus?.Id
            // UserId — TODO: przypiąć do zalogowanego użytkownika (z JWT)
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var pass = new SkiPass
        {
            CardId = req.CardId,
            TariffId = req.TariffId,
            ReservationId = reservation.Id,
            StatusId = activeStatus?.Id,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo
        };
        db.SkiPasses.Add(pass);

        var transaction = new Transaction
        {
            ReservationId = reservation.Id,
            OperationTypeId = opType?.Id,
            Amount = tariff.Price ?? 0,
            TransactionDate = DateTime.UtcNow
            // CashierId — TODO: z zalogowanej sesji kasjera
        };
        db.Transactions.Add(transaction);

        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = pass.Id }, ToDto(pass));
    }

    // POST /api/karnety/{id}/blokuj  — UC3
    [HttpPost("{id:int}/blokuj")]
    public async Task<IActionResult> BlockPass(int id, [FromBody] BlockPassRequest req)
    {
        var pass = await db.SkiPasses.FindAsync(id);
        if (pass == null) return NotFound();

        // TODO: pobrać id statusu "zablokowany"
        var blockedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "zablokowany");

        pass.StatusId = blockedStatus?.Id;
        pass.BlockReason = req.Reason;
        await db.SaveChangesAsync();

        return NoContent();
    }

    // POST /api/karnety/{id}/zwrot  — UC11
    [HttpPost("{id:int}/zwrot")]
    public async Task<IActionResult> ReturnPass(int id, [FromBody] ReturnPassRequest req)
    {
        var pass = await db.SkiPasses
            .Include(sp => sp.Tariff)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (pass == null) return NotFound();

        var preview = CalculateRefund(pass.ValidFrom, pass.ValidTo, pass.Tariff?.Price ?? 0, req.ReturnCard);

        // TODO: pobrać id statusu "zwrócony"
        var returnedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "zwrocony");
        // TODO: typ operacji "zwrot_karnetu"
        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "zwrot_karnetu");

        pass.StatusId = returnedStatus?.Id;
        pass.BlockReason = req.Reason;

        var refundTransaction = new Transaction
        {
            ReservationId = pass.ReservationId,
            OperationTypeId = opType?.Id,
            Amount = -preview.TotalRefund,   // kwota ujemna = wypłata
            TransactionDate = DateTime.UtcNow
        };
        db.Transactions.Add(refundTransaction);

        await db.SaveChangesAsync();

        return Ok(preview);
    }

    // GET /api/karnety/{id}/symulacja-zwrotu
    [HttpGet("{id:int}/symulacja-zwrotu")]
    public async Task<IActionResult> SimulateRefund(int id, [FromQuery] bool returnCard = false)
    {
        var pass = await db.SkiPasses
            .Include(sp => sp.Tariff)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (pass == null) return NotFound();

        var preview = CalculateRefund(pass.ValidFrom, pass.ValidTo, pass.Tariff?.Price ?? 0, returnCard);
        return Ok(preview);
    }

    private static ReturnPreviewDto CalculateRefund(
        DateTime? validFrom, DateTime? validTo, decimal totalPrice, bool returnCard)
    {
        const decimal ManipulationFee = 10m;
        const decimal CardDeposit = 20m;

        if (validFrom == null || validTo == null)
            return new ReturnPreviewDto(totalPrice, 0, 0, 0, ManipulationFee, 0, 0);

        var totalDays = (int)(validTo.Value - validFrom.Value).TotalDays;
        var usedDays = Math.Max(0, (int)(DateTime.UtcNow - validFrom.Value).TotalDays);
        var remainingDays = Math.Max(0, totalDays - usedDays);

        var pricePerDay = totalDays > 0 ? totalPrice / totalDays : 0;
        var refund = pricePerDay * remainingDays;
        var deposit = returnCard ? CardDeposit : 0;
        var total = Math.Max(0, refund - ManipulationFee + deposit);

        return new ReturnPreviewDto(totalPrice, totalDays, usedDays, refund, ManipulationFee, deposit, total);
    }

    private static PassDto ToDto(SkiPass sp) => new(
        sp.Id,
        sp.CardId ?? "",
        sp.Status?.Name,
        sp.Tariff?.Name,
        sp.Tariff?.PassType?.Name,
        sp.ValidFrom,
        sp.ValidTo,
        sp.BlockReason
    );
}

public record CreatePassRequest(string CardId, int TariffId, DateTime ValidFrom, DateTime ValidTo);
public record BlockPassRequest(string Reason);
public record ReturnPassRequest(string Reason, bool ReturnCard);

public record PassDto(
    int Id,
    string CardId,
    string? Status,
    string? Tariff,
    string? PassType,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string? BlockReason
);

public record ReturnPreviewDto(
    decimal GrossAmount,
    int TotalDays,
    int UsedDays,
    decimal RefundForUnusedDays,
    decimal ManipulationFee,
    decimal DepositReturn,
    decimal TotalRefund
);

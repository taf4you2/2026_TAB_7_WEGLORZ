using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/karnety")]
public class KarnetyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/karnety?cardId={rfid} - lista karnetow dla danej karty
    [HttpGet]
    public async Task<IActionResult> GetByCard([FromQuery] string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return BadRequest(new { message = "Parametr cardId jest wymagany." });

        await ExpireOutdatedPasses(cardId);

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

    // POST /api/karnety - UC2: Sprzedaj karnet
    [HttpPost]
    public async Task<IActionResult> CreatePass([FromBody] CreatePassRequest req)
    {
        var now = DateTime.UtcNow;
        var card = await db.Cards
            .Include(c => c.Status)
            .Include(c => c.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.CardId);
        if (card == null)
            return BadRequest(new { message = $"Karta {req.CardId} nie istnieje." });

        if (card.Status?.Name == "zajeta")
            return Conflict(new { message = "Karta jest juz zajeta." });

        if (!string.Equals(card.Status?.Name, "wolna", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = $"Karta ma status '{card.Status?.Name ?? "nieznany"}'." });

        var hasActivePass = card.SkiPasses.Any(sp =>
            sp.ValidFrom <= now &&
            sp.ValidTo >= now &&
            string.Equals(sp.Status?.Name, "aktywny", StringComparison.OrdinalIgnoreCase));
        if (hasActivePass)
            return Conflict(new { message = "Karta ma aktywny karnet." });

        var tariff = await db.Tariffs.FindAsync(req.TariffId);
        if (tariff == null)
            return BadRequest(new { message = "Taryfa nie istnieje." });

        if (req.UserId.HasValue && !await db.Users.AnyAsync(u => u.Id == req.UserId))
            return BadRequest(new { message = $"Uzytkownik {req.UserId} nie istnieje." });

        var activeStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "aktywny");
        var expiredStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "wygasly");
        var reservationStatus = await db.DictReservationStatuses.FirstOrDefaultAsync(s => s.Name == "potwierdzona");
        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "sprzedaz_karnetu");
        var passStatusId = req.ValidTo <= now && expiredStatus != null
            ? expiredStatus.Id
            : activeStatus?.Id;

        var reservation = new Reservation
        {
            ReservationNumber = $"RES-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            ReservationDate = DateTime.UtcNow,
            StatusId = reservationStatus?.Id,
            UserId = req.UserId
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var pass = new SkiPass
        {
            CardId = req.CardId,
            TariffId = req.TariffId,
            ReservationId = reservation.Id,
            StatusId = passStatusId,
            ValidFrom = DateTime.SpecifyKind(req.ValidFrom, DateTimeKind.Utc),
            ValidTo = DateTime.SpecifyKind(req.ValidTo, DateTimeKind.Utc),
            InitialRides = tariff.RideCount,
            RemainingRides = tariff.RideCount
        };
        db.SkiPasses.Add(pass);

        if (req.UserId.HasValue)
        {
            await db.Cards.Where(c => c.Id == req.CardId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.UserId, req.UserId.Value)
                    .SetProperty(p => p.DepositPaid, true));
        }
        else
        {
            await db.Cards.Where(c => c.Id == req.CardId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.DepositPaid, true));
        }

        var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var transaction = new Transaction
        {
            ReservationId = reservation.Id,
            CashierId = cashierId,
            OperationTypeId = opType?.Id,
            Amount = tariff.Price ?? 0,
            TransactionDate = DateTime.UtcNow
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        var zajetaId = await db.DictCardStatuses
            .Where(s => s.Name == "zajeta")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        if (zajetaId.HasValue)
            await db.Cards.Where(c => c.Id == req.CardId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StatusId, zajetaId.Value));

        return CreatedAtAction(nameof(GetById), new { id = pass.Id }, ToDto(pass));
    }

    // POST /api/karnety/{id}/blokuj - zostawione dla zgodnosci API
    // Post /api/karnety/zatwierdz-odbior
    [Authorize(Roles = "admin,kasjer")]
    [HttpPost("zatwierdz-odbior")]
    public async Task<IActionResult> GiveReservedCard([FromBody] ActivatePassRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.reservationNumber))
            return BadRequest(new { message = "Podaj numer rezerwacji." });

        if (string.IsNullOrWhiteSpace(req.cardRFID))
            return BadRequest(new { message = "Podaj RFID karty." });

        var managedReservation = await db.Reservations
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.ReservationNumber == req.reservationNumber);
        if (managedReservation == null)
            return BadRequest(new { message = "Nie znaleziono rezerwacji o takim numerze." });

        var statusId = await db.DictPassStatuses.Where(s => s.Name == "oczekuje_na_odbior").Select(s => s.Id).FirstOrDefaultAsync();
        if (statusId == 0)
            return BadRequest(new { message = "Nie znaleziono statusu oczekuje_na_odbior w slowniku, dodaj go." });

        var pendingPassesQuery = db.SkiPasses
            .Include(p => p.Status)
            .Include(p => p.Tariff)
            .Include(p => p.Reservation)
            .Where(p => p.ReservationId == managedReservation.Id && p.StatusId == statusId);

        SkiPass? managedPass;
        if (req.passId.HasValue)
        {
            managedPass = await pendingPassesQuery.FirstOrDefaultAsync(p => p.Id == req.passId.Value);
            if (managedPass == null)
                return BadRequest(new { message = "Nie znaleziono oczekującego karnetu w tej rezerwacji." });
        }
        else
        {
            var pendingPasses = await pendingPassesQuery.Take(2).ToListAsync();
            if (pendingPasses.Count == 0)
                return BadRequest(new { message = "Nie znaleziono rezerwacji lub karnetu do odbioru." });

            if (pendingPasses.Count > 1)
                return BadRequest(new { message = "Rezerwacja ma kilka karnetów do odbioru. Podaj passId." });

            managedPass = pendingPasses[0];
        }

        if (!managedPass.ValidTo.HasValue || managedPass.ValidTo.Value < DateTime.UtcNow)
            return BadRequest(new { message = "Data karnetu wygasła lub została utracona." });

        var activatedCard = await db.Cards
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.Id == req.cardRFID);
        if (activatedCard == null)
            return BadRequest(new { message = "Nie znaleziono karty o takim RFID." });

        if (!string.Equals(activatedCard.Status?.Name, "wolna", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = $"Karta ma status '{activatedCard.Status?.Name ?? "nieznany"}'." });

        var hasActivePass = await db.SkiPasses
            .Include(p => p.Status)
            .AnyAsync(p =>
                p.CardId == activatedCard.Id &&
                p.ValidFrom <= DateTime.UtcNow &&
                p.ValidTo >= DateTime.UtcNow &&
                p.Status != null &&
                p.Status.Name == "aktywny");
        if (hasActivePass)
            return Conflict(new { message = "Karta ma aktywny karnet." });

        var activePassStatusId = await db.DictPassStatuses
            .Where(s => s.Name == "aktywny")
            .Select(s => s.Id)
            .FirstOrDefaultAsync();
        if (activePassStatusId == 0)
            return BadRequest(new { message = "Nie znaleziono statusu aktywny w słowniku." });

        var occupiedCardStatusId = await db.DictCardStatuses
            .Where(s => s.Name == "zajeta")
            .Select(s => s.Id)
            .FirstOrDefaultAsync();
        if (occupiedCardStatusId == 0)
            return BadRequest(new { message = "Nie znaleziono statusu zajeta w słowniku." });

        managedPass.StatusId = activePassStatusId;
        managedPass.CardId = activatedCard.Id;
        activatedCard.UserId = managedReservation.UserId;
        activatedCard.DepositPaid = true;
        activatedCard.StatusId = occupiedCardStatusId;

        var hasOtherPendingPasses = await db.SkiPasses
            .AnyAsync(p => p.ReservationId == managedReservation.Id && p.StatusId == statusId && p.Id != managedPass.Id);
        if (!hasOtherPendingPasses)
        {
            managedReservation.StatusId = await db.DictReservationStatuses
                .Where(s => s.Name == "potwierdzona")
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
        }

        db.Transactions.Add(new Transaction
        {
            CashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            ReservationId = managedReservation.Id,
            OperationTypeId = await db.DictOperationTypes
                .Where(o => o.Name == "odbieranie_karnetu")
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(),
            Amount = managedPass.Tariff?.Price ?? 0,
            TransactionDate = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return Ok(new ReservedPassActivationResponse(
            managedReservation.Id,
            managedReservation.ReservationNumber,
            managedPass.Id,
            activatedCard.Id,
            "aktywny",
            managedPass.Tariff?.Name,
            managedPass.ValidFrom,
            managedPass.ValidTo,
            managedReservation.User?.Email));
    }

    [Authorize(Roles = "admin,kasjer")]
    [HttpGet("rezerwacje/{email}")]
    public async Task<IActionResult> GetReservationsByEmail(string email)
    {
        email = email.Trim();
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Podaj email narciarza." });

        var targetUser = await db.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
        if (targetUser != null)
        {
            var targetReservations = await db.Reservations.Where(r => r.UserId == targetUser.Id)
                .Include(rStatus => rStatus.Status)
                .Include(r => r.SkiPasses)
                .ThenInclude(sp => sp.Status)
                .Include(r => r.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
            
            var result = targetReservations.Select(r => new
            {
                r.Id,
                r.ReservationNumber,
                r.ReservationDate,
                status = r.Status?.Name,
                passes = r.SkiPasses.Select(sp => new
                {
                    sp.Id,
                    sp.CardId,
                    status = sp.Status?.Name,
                    tariff = sp.Tariff?.Name,
                    price  = sp.Tariff?.Price,
                    sp.ValidFrom,
                    sp.ValidTo
                })
            });
            
            return Ok(result);
            
        }
        return NotFound(new { message = $"Nie znaleziono narciarza o emailu {email}." });
    }
    
    
    
    // POST /api/karnety/{id}/blokuj  — UC3
    [HttpPost("{id:int}/blokuj")]
    public async Task<IActionResult> BlockPass(int id, [FromBody] BlockPassRequest req)
    {
        var pass = await db.SkiPasses.FindAsync(id);
        if (pass == null) return NotFound();

        var blockedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "zablokowany");

        pass.StatusId = blockedStatus?.Id;
        pass.BlockReason = req.Reason;
        await db.SaveChangesAsync();

        var wolnaId = await db.DictCardStatuses
            .Where(s => s.Name == "wolna")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        if (wolnaId.HasValue && pass.CardId != null)
            await db.Cards.Where(c => c.Id == pass.CardId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StatusId, wolnaId.Value));

        return NoContent();
    }

    // POST /api/karnety/{id}/zwrot - UC11
    [HttpPost("{id:int}/odblokuj")]
    public async Task<IActionResult> UnblockPass(int id)
    {
        var pass = await db.SkiPasses.FindAsync(id);
        if (pass == null) return NotFound();

        var activeStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "aktywny");
        pass.StatusId = activeStatus?.Id;
        pass.BlockReason = null;

        var zajetaId = await db.DictCardStatuses
            .Where(s => s.Name == "zajeta")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        if (zajetaId.HasValue && pass.CardId != null)
            await db.Cards.Where(c => c.Id == pass.CardId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.StatusId, zajetaId.Value));

        await db.SaveChangesAsync();
        return NoContent();
    }
    [HttpPost("{id:int}/zwrot")]
    public async Task<IActionResult> ReturnPass(int id, [FromBody] ReturnPassRequest req)
    {
        var pass = await db.SkiPasses
            .Include(sp => sp.Tariff)
            .Include(sp => sp.Card)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (pass == null) return NotFound();

        var (cardEligible, cardBlockReason) = await CheckCardReturnEligibility(pass.Id, pass.CardId);
        var depositPaid = pass.Card?.DepositPaid == true;
        var preview = CalculateRefund(pass.ValidFrom, pass.ValidTo, pass.Tariff?.Price ?? 0,
            req.ReturnCard, depositPaid, cardEligible, cardBlockReason);

        var returnedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "zwrocony");
        var refundOpType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "zwrot_karnetu");
        var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        pass.StatusId = returnedStatus?.Id;
        pass.BlockReason = req.Reason;

        var refundAmount = Math.Max(0, preview.RefundForUnusedDays - preview.ManipulationFee);
        db.Transactions.Add(new Transaction
        {
            ReservationId = pass.ReservationId,
            CashierId = cashierId,
            OperationTypeId = refundOpType?.Id,
            Amount = -refundAmount,
            TransactionDate = DateTime.UtcNow
        });

        if (req.ReturnCard && cardEligible && pass.Card != null)
        {
            var freeStatusId = await db.DictCardStatuses
                .Where(s => s.Name == "wolna")
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            pass.Card.StatusId = freeStatusId;
            pass.Card.UserId = null;
            pass.Card.BlockReason = null;
            pass.Card.DepositPaid = false;

            if (preview.DepositReturn > 0)
            {
                var depositOpType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "kaucja");
                db.Transactions.Add(new Transaction
                {
                    ReservationId = pass.ReservationId,
                    CashierId = cashierId,
                    OperationTypeId = depositOpType?.Id,
                    Amount = -preview.DepositReturn,
                    TransactionDate = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();

        return Ok(preview);
    }

    // GET /api/karnety/{id}/symulacja-zwrotu
    [HttpGet("{id:int}/symulacja-zwrotu")]
    public async Task<IActionResult> SimulateRefund(int id, [FromQuery] bool returnCard = false)
    {
        var pass = await db.SkiPasses
            .Include(sp => sp.Tariff)
            .Include(sp => sp.Card)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (pass == null) return NotFound();

        var (cardEligible, cardBlockReason) = await CheckCardReturnEligibility(pass.Id, pass.CardId);
        var depositPaid = pass.Card?.DepositPaid == true;
        var preview = CalculateRefund(pass.ValidFrom, pass.ValidTo, pass.Tariff?.Price ?? 0,
            returnCard, depositPaid, cardEligible, cardBlockReason);
        return Ok(preview);
    }
    // 
 
    private const decimal CardDepositAmount = 20m;
    private const decimal ManipulationFee = 10m;

    private static ReturnPreviewDto CalculateRefund(
        DateTime? validFrom,
        DateTime? validTo,
        decimal totalPrice,
        bool returnCard,
        bool depositPaid,
        bool cardReturnEligible,
        string? cardReturnBlockReason)
    {
        var depositReturn = returnCard && cardReturnEligible && depositPaid ? CardDepositAmount : 0m;

        if (validFrom == null || validTo == null)
            return new ReturnPreviewDto(totalPrice, 0, 0, 0, ManipulationFee, depositReturn, depositReturn, cardReturnEligible, cardReturnBlockReason);

        var totalDays = (int)(validTo.Value - validFrom.Value).TotalDays;
        var usedDays = Math.Max(0, (int)(DateTime.UtcNow - validFrom.Value).TotalDays);
        var remainingDays = Math.Max(0, totalDays - usedDays);

        var pricePerDay = totalDays > 0 ? totalPrice / totalDays : 0;
        var refund = pricePerDay * remainingDays;
        var total = Math.Max(0, refund - ManipulationFee) + depositReturn;

        return new ReturnPreviewDto(totalPrice, totalDays, usedDays, refund, ManipulationFee, depositReturn, total, cardReturnEligible, cardReturnBlockReason);
    }

    private async Task<(bool eligible, string? reason)> CheckCardReturnEligibility(int passId, string? cardId)
    {
        if (string.IsNullOrEmpty(cardId))
            return (false, "Karnet nie jest przypisany do karty.");

        var now = DateTime.UtcNow;
        var hasOtherActiveOrBlocked = await db.SkiPasses
            .Include(p => p.Status)
            .AnyAsync(p =>
                p.CardId == cardId &&
                p.Id != passId &&
                (p.Status!.Name == "zablokowany" ||
                 (p.Status.Name == "aktywny" && p.ValidFrom <= now && p.ValidTo >= now)));

        return hasOtherActiveOrBlocked
            ? (false, "Karta ma inne aktywne lub zablokowane karnety.")
            : (true, null);
    }

    private async Task ExpireOutdatedPasses(string cardId)
    {
        var expiredStatusId = await db.DictPassStatuses
            .Where(s => s.Name == "wygasly")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        var activeStatusId = await db.DictPassStatuses
            .Where(s => s.Name == "aktywny")
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (!expiredStatusId.HasValue || !activeStatusId.HasValue)
            return;

        await db.SkiPasses
            .Where(sp => sp.CardId == cardId && sp.StatusId == activeStatusId.Value && sp.ValidTo < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.StatusId, expiredStatusId.Value));
    }

    private static PassDto ToDto(SkiPass sp) => new(
        sp.Id,
        sp.CardId ?? "",
        sp.Status?.Name,
        sp.Tariff?.Name,
        sp.Tariff?.PassType?.Name,
        sp.ValidFrom,
        sp.ValidTo,
        sp.InitialRides,
        sp.RemainingRides,
        sp.BlockReason
    );

    // POST /api/karnety/specjalne
    [HttpPost("specjalne")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateSpecialPass([FromBody] SpecialPassRequest req)
    {
        var card = await db.Cards.FindAsync(req.CardId);
        if (card == null) return BadRequest("Karta nie istnieje.");

        var tariff = await db.Tariffs.FindAsync(req.TariffId);
        if (tariff == null) return BadRequest("Taryfa nie istnieje.");

        var activeStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "aktywny");
        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "wydanie_specjalne") ?? await db.DictOperationTypes.FirstOrDefaultAsync();

        var pass = new SkiPass
        {
            CardId = req.CardId,
            TariffId = req.TariffId,
            StatusId = activeStatus?.Id,
            ValidFrom = req.ValidFrom ?? DateTime.UtcNow,
            ValidTo = req.ValidTo ?? DateTime.UtcNow.AddYears(1), // Domyślnie rok dla VIP
            InitialRides = tariff.RideCount,
            RemainingRides = tariff.RideCount,
            BlockReason = "Specjalne: " + req.Reason
        };
        db.SkiPasses.Add(pass);

        var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var transaction = new Transaction
        {
            CashierId = cashierId,
            OperationTypeId = opType?.Id,
            Amount = 0,
            TransactionDate = DateTime.UtcNow
        };
        db.Transactions.Add(transaction);

        var zajetaId = await db.DictCardStatuses.FirstOrDefaultAsync(s => s.Name == "zajeta");
        if (zajetaId != null) card.StatusId = zajetaId.Id;
        
        await db.SaveChangesAsync();
        return Ok(ToDto(pass));
    }
}

public record ActivatePassRequest(string reservationNumber, string cardRFID, int? passId = null);
public record ReservedPassActivationResponse(
    int ReservationId,
    string ReservationNumber,
    int PassId,
    string CardId,
    string? PassStatus,
    string? Tariff,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string? OwnerEmail);
public record CreatePassRequest(string CardId, int TariffId, DateTime ValidFrom, DateTime ValidTo, int? UserId);
public record BlockPassRequest(string Reason);
public record ReturnPassRequest(string Reason, bool ReturnCard);
public record SpecialPassRequest(string CardId, int TariffId, string Reason, DateTime? ValidFrom, DateTime? ValidTo);

public record PassDto(
    int Id,
    string CardId,
    string? Status,
    string? Tariff,
    string? PassType,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int? InitialRides,
    int? RemainingRides,
    string? BlockReason
);

public record ReturnPreviewDto(
    decimal GrossAmount,
    int TotalDays,
    int UsedDays,
    decimal RefundForUnusedDays,
    decimal ManipulationFee,
    decimal DepositReturn,
    decimal TotalRefund,
    bool CardReturnEligible,
    string? CardReturnBlockReason
);



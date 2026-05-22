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
    [HttpPost("zatwierdz-odbior")]
    // Ten endpoint obsluguje wydawanie zarezerwowanych karnetów
    //TODO(Koniecznie należy napisać logikę zabezpieczającą przed overbookingiem)
    //Kasjerz podaje w requescie numer rezerwacji podany przez narciarza oraz RFID wolnej karty
    // Pytanie do frontendu ,  co endpoint mialby zwracac. Wydaje mi sie ze nie musi nic zwracac oprocz ok/nieok
    public async Task<IActionResult> GiveReservedCard([FromBody] ActivatePassRequest req)
    {
        //TODO ( Fix overbookingu , i przypisanie RFID karty do karnetu
        
        
        //Najpierw wyszukajmy tej rezerwacji i karnetu - dwa zapytania sql
        var managedReservation = await db.Reservations.Where(r => r.ReservationNumber == req.reservationNumber).FirstOrDefaultAsync();
        
        if (managedReservation==null)
        {
            return BadRequest(new { message = "Nie znaleziono rezerwacji o takim numerze" });
        }
        
        var statusId = await db.DictPassStatuses.Where(s => s.Name == "oczekuje_na_odbior").Select(s => s.Id).FirstOrDefaultAsync();
        if(statusId == 0)
            return BadRequest(new { message = "Nie znaleziono statusu oczekuje_na_odbior w slowniku, dodaj go" });
        var managedPasses = await db.SkiPasses
            .Include(p => p.Status)
            .Include(p => p.Tariff).Include(skiPass => skiPass.Reservation)
            .Where(p => (p.ReservationId == managedReservation.Id) &&
                        (p.StatusId==statusId)).ToListAsync<SkiPass>();
        if (managedPasses.Count() != 1)
        {
            return BadRequest(new { message = "Nie znaleziono rezerwacji lub karnetu." });
            
        }

        var managedPass = managedPasses.FirstOrDefault();
        var managedPassDate = managedPass.Reservation?.ReservationDate;
        

        //check for expiration date...
        if (managedPassDate != null && managedPass.ValidTo >= DateTime.UtcNow)
        {
            managedPass.StatusId = await db.DictPassStatuses
                .Where(s => s.Name == "aktywny")
                .Select(s => s.Id).FirstOrDefaultAsync();
            
            managedReservation.StatusId = await db.DictReservationStatuses
                .Where(s => s.Name == "potwierdzona")
                .Select(s => s.Id).FirstOrDefaultAsync();
            
            var activatedCard = await db.Cards
                .Where(c => c.Id == req.cardRFID)
                .FirstOrDefaultAsync();
            if (activatedCard == null)
            {
                return BadRequest(new { message = "Nie znaleziono karty o takim RFID" });
            }
            managedPass.CardId = activatedCard.Id;
            activatedCard.DepositPaid = true;

            activatedCard.StatusId = await db.DictCardStatuses
                .Where(s => s.Name == "zajeta")
                .Select(s => s.Id)
                .FirstOrDefaultAsync();
           int chashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var transaction = new Transaction
            {   CashierId = chashierId,
                ReservationId = managedReservation.Id,
                OperationTypeId = await db.DictOperationTypes
                    .Where(o => o.Name == "odbieranie_karnetu")
                    .Select(o => o.Id)
                    .FirstOrDefaultAsync(),
                Amount = managedPass.Tariff?.Price ?? 0,
                TransactionDate = DateTime.UtcNow
            };
            db.Transactions.Add(transaction);
            await db.SaveChangesAsync();
            return Ok();
        }
        else
        {
            return BadRequest(new { message = "Data karnetu wygasla lub zostala utracona " });
        }
        
        //Sprawdzmy czy rezerwacja istnieje i czy jest w terminie
        //Zmienmy status karnetu na aktywny i rezerwacji na potwierdzona
        //Zmieńmyy status karty na zajeta
        //Transkacja ok. Ale narciarz nie musi fizycznie placic
        
        
        return Ok();    
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
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (pass == null) return NotFound();

        var preview = CalculateRefund(pass.ValidFrom, pass.ValidTo, pass.Tariff?.Price ?? 0);

        var returnedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "zwrocony");
        // TODO: typ operacji "zwrot_karnetu"
        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "zwrot_karnetu");

        pass.StatusId = returnedStatus?.Id;
        pass.BlockReason = req.Reason;

        
   
        var refundTransaction = new Transaction
        {
            ReservationId = pass.ReservationId,
            CashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            OperationTypeId = opType?.Id,
            Amount = -preview.TotalRefund,
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

        var preview = CalculateRefund(pass.ValidFrom, pass.ValidTo, pass.Tariff?.Price ?? 0);
        return Ok(preview);
    }
    // 
 
    private static ReturnPreviewDto CalculateRefund(
        DateTime? validFrom, DateTime? validTo, decimal totalPrice)
    {
        const decimal ManipulationFee = 10m;

        if (validFrom == null || validTo == null)
            return new ReturnPreviewDto(totalPrice, 0, 0, 0, ManipulationFee, 0, 0);

        var totalDays = (int)(validTo.Value - validFrom.Value).TotalDays;
        var usedDays = Math.Max(0, (int)(DateTime.UtcNow - validFrom.Value).TotalDays);
        var remainingDays = Math.Max(0, totalDays - usedDays);

        var pricePerDay = totalDays > 0 ? totalPrice / totalDays : 0;
        var refund = pricePerDay * remainingDays;
        var deposit = 0m;
        var total = Math.Max(0, refund - ManipulationFee);

        return new ReturnPreviewDto(totalPrice, totalDays, usedDays, refund, ManipulationFee, deposit, total);
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
}

public record ActivatePassRequest(string reservationNumber, string cardRFID);
public record CreatePassRequest(string CardId, int TariffId, DateTime ValidFrom, DateTime ValidTo, int? UserId);
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
    decimal TotalRefund
);



using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemAPI.Services;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/zakup")]
public class ZakupOnlineController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/zakup/taryfy — publiczny, zwraca karnety (nie bilety jednorazowe)
    [HttpGet("taryfy")]
    public async Task<IActionResult> GetTaryfy()
    {
        var taryfy = await db.Tariffs
            .Include(t => t.Season)
            .Include(t => t.PassType)
            .Where(t => (t.IsActive == true || t.IsActive == null)
                && t.PassType != null
                && t.PassType.Name.StartsWith("karnet"))
            .OrderBy(t => t.Price)
            .ToListAsync();

        return Ok(taryfy.Select(t => new
        {
            t.Id,
            t.Name,
            season = t.Season?.Name,
            t.Price ,
            t.RideCount,
            durationDays = GetDurationDays(t.Name)
        }));
    }

    // POST /api/zakup/online — UC2: zakup online przez narciarza
    // Body: { "tariffId": 13, "validFrom": "2026-05-10" }
    [Authorize]
    [HttpPost("online")]
    public async Task<IActionResult> KupOnline([FromBody] ZakupOnlineRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var requestedCardId = req.CardId?.Trim();

        var tariff = await db.Tariffs.Include(t => t.PassType).FirstOrDefaultAsync(t => t.Id == req.TariffId);
        if (tariff == null)
            return BadRequest(new { message = "Wybrana taryfa nie istnieje." });
        if (tariff.IsActive == false)
            return Conflict(new { message = "Wybrana taryfa jest nieaktywna." });

        if (tariff.PassType?.Name?.StartsWith("karnet") != true)
            return BadRequest(new { message = "Zakup online dotyczy wyłącznie karnetów." });

        var durationDays = GetDurationDays(tariff.Name);
        var validFrom = req.ValidFrom.Date;
        var validTo = validFrom.AddDays(durationDays);
        var saleWindow = await SalesDatePolicy.GetMinimumSaleDateAsync(db);

        if (validFrom < saleWindow.MinimumDate)
            return BadRequest(new { message = SalesDatePolicy.CreateTooEarlyMessage("karnet", saleWindow) });

        Card? assignedCard = null;
        if (!string.IsNullOrWhiteSpace(requestedCardId))
        {
            assignedCard = await db.Cards
                .Include(c => c.Status)
                .Include(c => c.SkiPasses)
                    .ThenInclude(sp => sp.Status)
                .FirstOrDefaultAsync(c => c.Id == requestedCardId);

            if (assignedCard == null)
                return BadRequest(new { message = $"Karta {requestedCardId} nie istnieje." });

            if (string.Equals(assignedCard.Status?.Name, "zastrzezony", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = "Karta jest zastrzezona." });

            if (HasBlockingPassInPeriod(assignedCard, validFrom, validTo))
                return Conflict(new { message = "Karta RFID ma juz przypisany karnet w wybranym okresie." });
        }

        if (tariff.PoolLimit.HasValue)
        {
            //TODO(Nalezy sprawdzic czy zablokowane karnety powinny sie zaliczac do puli biletów!!!)
            var totalReservationsForTarrif = await db.SkiPasses.CountAsync(p => p.TariffId == req.TariffId);
            if (totalReservationsForTarrif + 1 > tariff.PoolLimit.Value)
                return BadRequest(new { message = "Wszystkie miejsca zostaly wyczerpane" });
        }
        var oczekujacaStatus = await db.DictReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "oczekujaca");
        var oczekujeNaOdbiórStatus = await db.DictPassStatuses
            .FirstOrDefaultAsync(s => s.Name == "oczekuje_na_odbior");

        
        var potwierdzonaStatus = await db.DictReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "potwierdzona");
        var aktywnyStatus = await db.DictPassStatuses
            .FirstOrDefaultAsync(s => s.Name == "aktywny");
        var zajetaStatus = await db.DictCardStatuses
            .FirstOrDefaultAsync(s => s.Name == "zajeta");

        if (assignedCard != null && (aktywnyStatus == null || zajetaStatus == null))
            return BadRequest(new { message = "Brakuje statusu aktywny lub zajeta w slowniku." });

        var reservation = new Reservation
        {
            ReservationNumber = $"ONL-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            ReservationDate = DateTime.UtcNow,
            StatusId = assignedCard == null ? oczekujacaStatus?.Id : potwierdzonaStatus?.Id,
            UserId = userId
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        
    
        var pass = new SkiPass
        {
            CardId = assignedCard?.Id,
            TariffId = req.TariffId,
            ReservationId = reservation.Id,
            StatusId = oczekujeNaOdbiórStatus?.Id,
            ValidFrom = DateTime.SpecifyKind(validFrom, DateTimeKind.Utc),
            ValidTo = DateTime.SpecifyKind(validTo, DateTimeKind.Utc),
            InitialRides = tariff.RideCount,
            RemainingRides = tariff.RideCount
        };
        db.SkiPasses.Add(pass);

        if (assignedCard != null)
        {
            pass.StatusId = aktywnyStatus?.Id;
            assignedCard.UserId = userId;
            assignedCard.DepositPaid = true;
            assignedCard.StatusId = zajetaStatus?.Id;
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            reservationNumber = reservation.ReservationNumber,
            passId = pass.Id,
            tariff = tariff.Name,
            price = tariff.Price,
            rideCount = tariff.RideCount,
            durationDays,
            cardId = pass.CardId,
            passStatus = assignedCard == null ? "oczekuje_na_odbior" : aktywnyStatus?.Name,
            validFrom = pass.ValidFrom,
            validTo = pass.ValidTo,
            message = assignedCard == null
                ? "Rezerwacja przyjęta. Odbierz karnet przy kasie, podając numer rezerwacji."
                : "Karnet zostal przypisany do podanej karty RFID."
        });
    }

    private static int GetDurationDays(string tariffName)
    {
        var match = Regex.Match(
            tariffName,
            @"\b(\d+)[-\s]*dniow",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success
            && int.TryParse(match.Groups[1].Value, out var days)
            && days > 0
            ? days
            : 1;
    }

    private static bool HasBlockingPassInPeriod(Card card, DateTime validFrom, DateTime validTo)
    {
        return card.SkiPasses.Any(sp =>
            sp.ValidFrom.HasValue &&
            sp.ValidTo.HasValue &&
            sp.ValidFrom.Value < validTo &&
            sp.ValidTo.Value > validFrom &&
            !string.Equals(sp.Status?.Name, "wygasly", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sp.Status?.Name, "zwrocony", StringComparison.OrdinalIgnoreCase));
    }
}

public record ZakupOnlineRequest(int TariffId, DateTime ValidFrom, string? CardId = null);

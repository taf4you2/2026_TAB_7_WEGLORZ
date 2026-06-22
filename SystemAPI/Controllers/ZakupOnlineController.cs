using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        var tariff = await db.Tariffs.Include(t => t.PassType).FirstOrDefaultAsync(t => t.Id == req.TariffId);
        if (tariff == null)
            return BadRequest(new { message = "Wybrana taryfa nie istnieje." });
        if (tariff.IsActive == false)
            return Conflict(new { message = "Wybrana taryfa jest nieaktywna." });

        if (tariff.PassType?.Name?.StartsWith("karnet") != true)
            return BadRequest(new { message = "Zakup online dotyczy wyłącznie karnetów." });

        var durationDays = GetDurationDays(tariff.Name);
        var validFrom = DateTime.SpecifyKind(req.ValidFrom.Date, DateTimeKind.Utc);
        var validTo = validFrom.AddDays(durationDays);

        // Nie pozwalamy kupić karnetu z datą rozpoczęcia wcześniejszą niż dzisiaj.
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        if (validFrom < today)
            return BadRequest(new { message = "Data rozpoczęcia nie może być wcześniejsza niż dzisiejsza." });

        if (tariff.PoolLimit.HasValue)
        {
            //TODO(Nalezy sprawdzic czy zablokowane karnety powinny sie zaliczac do puli biletów!!!)
            var totalReservationsForTarrif = await db.SkiPasses.CountAsync(p => p.TariffId == req.TariffId);
            if (totalReservationsForTarrif + 1 > tariff.PoolLimit.Value)
                return BadRequest(new { message = "Wszystkie miejsca zostaly wyczerpane" });
        }

        // ── Wariant A: zakup na WŁASNĄ kartę narciarza → karnet od razu aktywny (bez odbioru przy kasie) ──
        if (!string.IsNullOrWhiteSpace(req.CardId))
        {
            var card = await db.Cards.Include(c => c.Status).FirstOrDefaultAsync(c => c.Id == req.CardId);
            if (card == null)
                return BadRequest(new { message = "Wskazana karta nie istnieje." });
            if (card.UserId != userId)
                return BadRequest(new { message = "Ta karta nie należy do Ciebie." });

            var hasOverlap = await db.SkiPasses
                .Include(p => p.Status)
                .AnyAsync(p => p.CardId == card.Id
                    && p.Status != null && p.Status.Name == "aktywny"
                    && p.ValidFrom < validTo && p.ValidTo > validFrom);
            if (hasOverlap)
                return Conflict(new { message = "Na tej karcie istnieje już aktywny karnet w wybranym terminie." });

            var potwierdzonaId = await db.DictReservationStatuses
                .Where(s => s.Name == "potwierdzona").Select(s => (int?)s.Id).FirstOrDefaultAsync();
            var aktywnyId = await db.DictPassStatuses
                .Where(s => s.Name == "aktywny").Select(s => (int?)s.Id).FirstOrDefaultAsync();

            var ownReservation = new Reservation
            {
                ReservationNumber = $"ONL-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                ReservationDate = DateTime.UtcNow,
                StatusId = potwierdzonaId,
                UserId = userId
            };
            db.Reservations.Add(ownReservation);
            await db.SaveChangesAsync();

            var activePass = new SkiPass
            {
                CardId = card.Id,
                TariffId = req.TariffId,
                ReservationId = ownReservation.Id,
                StatusId = aktywnyId,
                ValidFrom = validFrom,
                ValidTo = validTo,
                InitialRides = tariff.RideCount,
                RemainingRides = tariff.RideCount
            };
            db.SkiPasses.Add(activePass);

            // Karta staje się zajęta; kaucja była już opłacona przy pierwszym wydaniu karty.
            var zajetaId = await db.DictCardStatuses
                .Where(s => s.Name == "zajeta").Select(s => (int?)s.Id).FirstOrDefaultAsync();
            if (zajetaId.HasValue) card.StatusId = zajetaId.Value;

            // Sprzedaż online — bez kasjera (cashier_id = NULL, inaczej FK transaction_cashier_id_fkey).
            var saleOpId = await db.DictOperationTypes
                .Where(o => o.Name == "sprzedaz_karnetu").Select(o => (int?)o.Id).FirstOrDefaultAsync();
            db.Transactions.Add(new Transaction
            {
                ReservationId = ownReservation.Id,
                CashierId = null,
                OperationTypeId = saleOpId,
                Amount = tariff.Price ?? 0,
                TransactionDate = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            return Ok(new
            {
                reservationNumber = ownReservation.ReservationNumber,
                passId = activePass.Id,
                cardId = card.Id,
                tariff = tariff.Name,
                price = tariff.Price,
                rideCount = tariff.RideCount,
                durationDays,
                validFrom = activePass.ValidFrom,
                validTo = activePass.ValidTo,
                status = "aktywny",
                message = "Karnet został kupiony i jest już aktywny na Twojej karcie."
            });
        }

        // ── Wariant B: brak karty → rezerwacja do odbioru przy kasie (dotychczasowy przepływ) ──
        var oczekujacaStatus = await db.DictReservationStatuses
            .FirstOrDefaultAsync(s => s.Name == "oczekujaca");
        var oczekujeNaOdbiórStatus = await db.DictPassStatuses
            .FirstOrDefaultAsync(s => s.Name == "oczekuje_na_odbior");

        
        var reservation = new Reservation
        {
            ReservationNumber = $"ONL-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            ReservationDate = DateTime.UtcNow,
            StatusId = oczekujacaStatus?.Id,
            UserId = userId
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        
    
        var pass = new SkiPass
        {
            TariffId = req.TariffId,
            ReservationId = reservation.Id,
            StatusId = oczekujeNaOdbiórStatus?.Id,
            ValidFrom = validFrom,
            ValidTo = validTo,
            InitialRides = tariff.RideCount,
            RemainingRides = tariff.RideCount
        };
        db.SkiPasses.Add(pass);
        await db.SaveChangesAsync();

        return Ok(new
        {
            reservationNumber = reservation.ReservationNumber,
            passId = pass.Id,
            tariff = tariff.Name,
            price = tariff.Price,
            rideCount = tariff.RideCount,
            durationDays,
            validFrom = pass.ValidFrom,
            validTo = pass.ValidTo,
            message = "Rezerwacja przyjęta. Odbierz karnet przy kasie, podając numer rezerwacji."
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
}

public record ZakupOnlineRequest(int TariffId, DateTime ValidFrom, string? CardId = null);

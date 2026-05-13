using System.Security.Claims;
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
            .Where(t => t.PassType != null && t.PassType.Name.StartsWith("karnet"))
            .OrderBy(t => t.Price)
            .ToListAsync();

        return Ok(taryfy.Select(t => new
        {
            t.Id,
            t.Name,
            season = t.Season?.Name,
            t.Price,
            t.RideCount
        }));
    }

    // POST /api/zakup/online — UC2: zakup online przez narciarza
    // Body: { "tariffId": 5, "validFrom": "2026-05-10", "validTo": "2026-05-12" }
    [Authorize]
    [HttpPost("online")]
    public async Task<IActionResult> KupOnline([FromBody] ZakupOnlineRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var tariff = await db.Tariffs.Include(t => t.PassType).FirstOrDefaultAsync(t => t.Id == req.TariffId);
        if (tariff == null)
            return BadRequest(new { message = "Wybrana taryfa nie istnieje." });

        if (tariff.PassType?.Name?.StartsWith("karnet") != true)
            return BadRequest(new { message = "Zakup online dotyczy wyłącznie karnetów." });

        if (req.ValidFrom >= req.ValidTo)
            return BadRequest(new { message = "Data końca musi być późniejsza niż data początku." });

        var tarrifLimit = tariff.PoolLimit;
        if (tarrifLimit == null)
        {
            return BadRequest(new { message = "Unexpected error, are you trying to hack us?" });
        }
        //TODO(Nalezy sprawdzic czy zablokowane karnety powinny sie zaliczac do puli biletów!!!)
        var totalReservationsForTarrif = await db.SkiPasses.CountAsync(p => p.TariffId == req.TariffId );
        
        if (totalReservationsForTarrif + 1 > tarrifLimit)
        {
            return BadRequest(new { message = "Wszystkie miejsca zostaly wyczerpane" });
        }
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
            ValidFrom = DateTime.SpecifyKind(req.ValidFrom, DateTimeKind.Utc),
            ValidTo = DateTime.SpecifyKind(req.ValidTo, DateTimeKind.Utc),
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
            validFrom = pass.ValidFrom,
            validTo = pass.ValidTo,
            message = "Rezerwacja przyjęta. Odbierz karnet przy kasie, podając numer rezerwacji."
        });
    }
}

public record ZakupOnlineRequest(int TariffId, DateTime ValidFrom, DateTime ValidTo);

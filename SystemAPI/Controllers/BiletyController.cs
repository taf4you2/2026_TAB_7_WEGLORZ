using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/bilety")]
public class BiletyController(SkiResortDbContext db) : ControllerBase
{
    // POST /api/bilety  — UC1: Sprzedaj bilet jednorazowy
    // Jeden bilet = jeden SkiPass z taryfą jednorazową, ważny na podany dzień.
    // Quantity > 1 tworzy wiele rekordów SkiPass na tej samej karcie/rezerwacji.
    [HttpPost]
    public async Task<IActionResult> SellTicket([FromBody] SellTicketRequest req)
    {
        if (req.Quantity < 1 || req.Quantity > 50)
            return BadRequest(new { message = "Liczba biletów musi być między 1 a 50." });

        var card = await db.Cards.FindAsync(req.CardId);
        if (card == null)
            return BadRequest(new { message = $"Karta {req.CardId} nie istnieje." });

        var tariff = await db.Tariffs.FindAsync(req.TariffId);
        if (tariff == null)
            return BadRequest(new { message = "Taryfa nie istnieje." });

        // TODO: status "aktywny", status rezerwacji "potwierdzona", typ operacji "sprzedaz_biletu"
        var activeStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "aktywny");
        var reservationStatus = await db.DictReservationStatuses.FirstOrDefaultAsync(s => s.Name == "potwierdzona");
        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "sprzedaz_biletu");

        var reservation = new Reservation
        {
            ReservationNumber = $"RES-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            ReservationDate = DateTime.UtcNow,
            StatusId = reservationStatus?.Id
            // UserId — TODO: z sesji kasjera
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var validFrom = req.ValidOn.Date.ToUniversalTime();
        var validTo = validFrom.AddDays(1);

        for (var i = 0; i < req.Quantity; i++)
        {
            db.SkiPasses.Add(new SkiPass
            {
                CardId = req.CardId,
                TariffId = req.TariffId,
                ReservationId = reservation.Id,
                StatusId = activeStatus?.Id,
                ValidFrom = validFrom,
                ValidTo = validTo
            });
        }

        db.Transactions.Add(new Transaction
        {
            ReservationId = reservation.Id,
            OperationTypeId = opType?.Id,
            Amount = (tariff.Price ?? 0) * req.Quantity,
            TransactionDate = DateTime.UtcNow
            // CashierId — TODO: z sesji
        });

        await db.SaveChangesAsync();

        return Ok(new
        {
            ReservationId = reservation.Id,
            Quantity = req.Quantity,
            TotalAmount = (tariff.Price ?? 0) * req.Quantity,
            ValidOn = req.ValidOn.Date
        });
    }
}

record SellTicketRequest(string CardId, int TariffId, DateTime ValidOn, int Quantity);

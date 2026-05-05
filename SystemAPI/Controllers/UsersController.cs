using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/users/me
    // Zwraca profil zalogowanego narciarza i jego karty RFID.
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await db.Users
            .Include(u => u.Reservations)
                .ThenInclude(r => r.SkiPasses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        var cardIds = user.Reservations
            .SelectMany(r => r.SkiPasses)
            .Select(sp => sp.CardId)
            .Where(id => id != null)
            .Distinct()
            .ToList();

        return Ok(new { userId = user.Id, email = user.Email, cardIds });
    }

    // GET /api/users/me/rezerwacje
    // Zwraca rezerwacje zalogowanego narciarza wraz z karnetami (w tym oczekujące na odbiór).
    [HttpGet("me/rezerwacje")]
    public async Task<IActionResult> GetMyReservations()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var reservations = await db.Reservations
            .Include(r => r.Status)
            .Include(r => r.SkiPasses)
                .ThenInclude(sp => sp.Status)
            .Include(r => r.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();

        var result = reservations.Select(r => new
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
}

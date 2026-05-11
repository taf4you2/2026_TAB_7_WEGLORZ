using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

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

    // GET /api/uzytkownicy?email=  — dla KasjerApp: wyszukiwanie narciarza po emailu
    [HttpGet("/api/uzytkownicy")]
    public async Task<IActionResult> Search([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Parametr email jest wymagany." });

        var users = await db.Users
            .Where(u => u.Email!.Contains(email))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("/api/uzytkownicy")]
    public async Task<IActionResult> CreateSkier([FromBody] CreateUserRequest req)
    {
        var email = req.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email jest wymagany." });

        var existing = await db.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
        if (existing != null)
            return Ok(new UserDto(existing.Id, existing.Email ?? email));

        var user = new User
        {
            Email = email,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created(string.Empty, new UserDto(user.Id, user.Email ?? email));
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

    // GET /api/users/all
    // Zwraca listę wszystkich użytkowników systemu (administratorzy, kasjerzy, narciarze).
    // Tylko dla admina i kasjera (dostęp do panelu).
    [Authorize(Roles = "admin,kasjer")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        var admins = await db.Administrators
            .Select(a => new { a.Id, Login = a.Login, Role = "admin", IsActive = a.IsActive ?? true })
            .ToListAsync();

        var cashiers = await db.Cashiers
            .Select(c => new { c.Id, Login = c.Login, Role = "kasjer", IsActive = c.IsActive ?? true })
            .ToListAsync();

        var users = await db.Users
            .Select(u => new { u.Id, Login = u.Email, Role = "narciarz", IsActive = true })
            .ToListAsync();

        var allUsers = admins.Cast<object>()
            .Concat(cashiers.Cast<object>())
            .Concat(users.Cast<object>())
            .ToList();

        return Ok(allUsers);
    }
}

public record CreateUserRequest(string Email);
public record UserDto(int Id, string Email);

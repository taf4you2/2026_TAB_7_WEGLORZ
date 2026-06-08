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
    // Tylko dla admina (dostep do panelu administratora).
    [Authorize(Roles = "admin")]
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

    // POST /api/users
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateUser([FromBody] UserModifyRequest req)
    {
        var validationError = await ValidateStaffRequest(req, requirePassword: true);
        if (validationError != null) return validationError;

        if (req.Role == "admin")
        {
            var admin = new Administrator
            {
                Login = req.Login.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password!),
                IsActive = req.IsActive
            };
            db.Administrators.Add(admin);
        }
        else if (req.Role == "kasjer")
        {
            var cashier = new Cashier
            {
                Login = req.Login.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password!),
                IsActive = req.IsActive
            };
            db.Cashiers.Add(cashier);
        }
        else
        {
            return BadRequest("Nieprawidłowa rola. Dozwolone: admin, kasjer.");
        }

        await db.SaveChangesAsync();
        return Ok();
    }

    // PUT /api/users/{id}?role={role}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateUser(int id, [FromQuery] string? role, [FromBody] UserModifyRequest req)
    {
        if (string.IsNullOrWhiteSpace(role))
            return BadRequest(new { message = "Parametr role jest wymagany przy edycji pracownika." });

        role = role.Trim();
        if (role != req.Role)
            return BadRequest(new { message = "Nie mozna zmienic roli pracownika podczas edycji." });

        var validationError = await ValidateStaffRequest(req, requirePassword: false, currentId: id);
        if (validationError != null) return validationError;

        if (role == "admin")
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == currentAdminId && !req.IsActive)
                return BadRequest(new { message = "Nie mozna dezaktywowac wlasnego konta administratora." });

            var admin = await db.Administrators.FindAsync(id);
            if (admin == null) return NotFound();

            admin.Login = req.Login.Trim();
            admin.IsActive = req.IsActive;
            if (!string.IsNullOrEmpty(req.Password))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            }
        }
        else if (role == "kasjer")
        {
            var cashier = await db.Cashiers.FindAsync(id);
            if (cashier == null) return NotFound();

            cashier.Login = req.Login.Trim();
            cashier.IsActive = req.IsActive;
            if (!string.IsNullOrEmpty(req.Password))
            {
                cashier.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
            }
        }
        else
        {
            return BadRequest("Nieprawidłowa rola.");
        }

        await db.SaveChangesAsync();
        return Ok();
    }

    // GET /api/users/{id}/history
    // Zwraca historię rezerwacji i transakcji dla konkretnego użytkownika.
    [Authorize(Roles = "admin")]
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetUserHistory(int id)
    {
        var reservations = await db.Reservations
            .Include(r => r.Status)
            .Include(r => r.SkiPasses)
                .ThenInclude(sp => sp.Tariff)
            .Include(r => r.Transactions)
                .ThenInclude(t => t.OperationType)
            .Where(r => r.UserId == id)
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();

        var result = new
        {
            Reservations = reservations.Select(r => new
            {
                r.Id,
                r.ReservationNumber,
                r.ReservationDate,
                Status = r.Status?.Name,
                Passes = r.SkiPasses.Select(sp => new
                {
                    sp.Id,
                    sp.CardId,
                    Tariff = sp.Tariff?.Name,
                    sp.ValidFrom,
                    sp.ValidTo
                }),
                Transactions = r.Transactions.Select(t => new
                {
                    t.Id,
                    OperationType = t.OperationType?.Name,
                    t.Amount,
                    t.TransactionDate
                })
            })
        };

        return Ok(result);
    }

    // DELETE /api/users/{id}?role={role}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery] string role)
    {
        if (role == "admin")
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == currentAdminId)
                return BadRequest(new { message = "Nie mozna usunac wlasnego konta administratora." });

            var admin = await db.Administrators.FindAsync(id);
            if (admin == null) return NotFound();
            db.Administrators.Remove(admin);
        }
        else if (role == "kasjer")
        {
            var cashier = await db.Cashiers.Include(c => c.Transactions).Include(c => c.ShiftReports).FirstOrDefaultAsync(c => c.Id == id);
            if (cashier == null) return NotFound();
            
            if (cashier.Transactions.Any() || cashier.ShiftReports.Any())
            {
                // Zamiast usuwania, po prostu deaktywujemy, jeśli ma transakcje
                cashier.IsActive = false;
            }
            else
            {
                db.Cashiers.Remove(cashier);
            }
        }
        else
        {
            return BadRequest("Nieprawidłowa rola.");
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<IActionResult?> ValidateStaffRequest(UserModifyRequest req, bool requirePassword, int? currentId = null)
    {
        var login = req.Login?.Trim();
        if (string.IsNullOrWhiteSpace(login))
            return BadRequest(new { message = "Login jest wymagany." });

        if (req.Role is not ("admin" or "kasjer"))
            return BadRequest(new { message = "Nieprawidlowa rola. Dozwolone: admin, kasjer." });

        if (requirePassword && string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Haslo jest wymagane przy tworzeniu pracownika." });

        if (!string.IsNullOrWhiteSpace(req.Password) && req.Password.Length < 8)
            return BadRequest(new { message = "Haslo musi miec co najmniej 8 znakow." });

        var duplicateAdmin = await db.Administrators
            .AnyAsync(a => a.Login == login && (!currentId.HasValue || req.Role != "admin" || a.Id != currentId.Value));
        var duplicateCashier = await db.Cashiers
            .AnyAsync(c => c.Login == login && (!currentId.HasValue || req.Role != "kasjer" || c.Id != currentId.Value));

        if (duplicateAdmin || duplicateCashier)
            return Conflict(new { message = "Pracownik o takim loginie juz istnieje." });

        return null;
    }
}

public record UserModifyRequest(
    string Login,
    string? Password,
    string Role,
    bool IsActive
);


public record CreateUserRequest(string Email);
public record UserDto(int Id, string Email);

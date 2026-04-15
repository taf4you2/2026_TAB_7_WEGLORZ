using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(SkiResortDbContext db) : ControllerBase
{
    // POST /api/auth/login
    // Body: { "email": "kasjer@stacja.pl", "password": "...", "role": "kasjer"|"narciarz" }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (req.Role == "kasjer")
        {
            var cashier = await db.Cashiers
                .FirstOrDefaultAsync(c => c.Login == req.Email && c.IsActive == true);

            if (cashier == null)
                return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });

            // TODO: weryfikacja hasła (BCrypt / PBKDF2)
            // if (!PasswordHasher.Verify(req.Password, cashier.PasswordHash)) return Unauthorized(...);

            // TODO: wygenerować JWT lub session token
            return Ok(new LoginResponse(cashier.Id, "kasjer", "stub-token"));
        }
        else if (req.Role == "narciarz")
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
                return Unauthorized(new { message = "Nieprawidłowy e-mail lub hasło." });

            // TODO: weryfikacja hasła
            return Ok(new LoginResponse(user.Id, "narciarz", "stub-token"));
        }

        return BadRequest(new { message = "Nieznana rola." });
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // TODO: unieważnienie tokenu JWT / sesji
        return NoContent();
    }
}

record LoginRequest(string Email, string Password, string Role);
record LoginResponse(int UserId, string Role, string Token);

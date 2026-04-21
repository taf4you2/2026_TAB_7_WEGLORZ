using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(SkiResortDbContext db, IConfiguration config) : ControllerBase
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

            if (cashier == null || !BCrypt.Net.BCrypt.Verify(req.Password, cashier.PasswordHash))
                return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });

            return Ok(new LoginResponse(cashier.Id, "kasjer", GenerateToken(cashier.Id, "kasjer")));
        }
        else if (req.Role == "narciarz")
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { message = "Nieprawidłowy e-mail lub hasło." });

            return Ok(new LoginResponse(user.Id, "narciarz", GenerateToken(user.Id, "narciarz")));
        }

        return BadRequest(new { message = "Nieznana rola." });
    }

    // POST /api/auth/logout
    // JWT jest bezstanowy — wylogowanie po stronie klienta (usunięcie tokenu).
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    private string GenerateToken(int userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSecret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Email, string Password, string Role);
public record LoginResponse(int UserId, string Role, string Token);

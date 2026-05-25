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
                .FirstOrDefaultAsync(c => c.Login == req.Email && (c.IsActive == true || c.IsActive == null));

            if (cashier == null || !BCrypt.Net.BCrypt.Verify(req.Password, cashier.PasswordHash))
                return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });

            return Ok(new LoginResponse(cashier.Id, "kasjer", GenerateToken(cashier.Id, "kasjer", cashier.Login)));
        }
        else if (req.Role == "admin")
        {
            var admin = await db.Administrators
                .FirstOrDefaultAsync(a => a.Login == req.Email && (a.IsActive == true || a.IsActive == null));

            if (admin == null || !BCrypt.Net.BCrypt.Verify(req.Password, admin.PasswordHash))
                return Unauthorized(new { message = "Nieprawidłowy login lub hasło." });

            return Ok(new LoginResponse(admin.Id, "admin", GenerateToken(admin.Id, "admin", admin.Login)));
        }
        else if (req.Role == "narciarz")
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Unauthorized(new { message = "Nieprawidłowy e-mail lub hasło." });

            return Ok(new LoginResponse(user.Id, "narciarz", GenerateToken(user.Id, "narciarz", user.Email)));
        }

        return BadRequest(new { message = "Nieznana rola." });
    }

    // POST /api/auth/register
    // Body: { "email": "...", "password": "..." }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "E-mail i hasło są wymagane." });

        if (req.Password.Length < 8)
            return BadRequest(new { message = "Hasło musi mieć co najmniej 8 znaków." });

        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "Podany adres e-mail jest już zarejestrowany." });

        var user = new SystemStacjiNarciarskiejDLL.Models.User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new LoginResponse(user.Id, "narciarz", GenerateToken(user.Id, "narciarz", user.Email)));
    }

    // POST /api/auth/logout
    // JWT jest bezstanowy — wylogowanie po stronie klienta (usunięcie tokenu).
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    private string GenerateToken(int userId, string role, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSecret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, name),
            new Claim("unique_name", name)
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
public record RegisterRequest(string Email, string Password);
public record LoginResponse(int UserId, string Role, string Token);

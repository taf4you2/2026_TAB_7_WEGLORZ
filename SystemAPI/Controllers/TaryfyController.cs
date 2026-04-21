using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/taryfy")]
public class TaryfyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/taryfy
    // Zwraca listę dostępnych taryf wraz z typem karnetu i sezonem.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var taryfy = await db.Tariffs
            .Include(t => t.Season)
            .Include(t => t.PassType)
            .OrderBy(t => t.PassTypeId)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var result = taryfy.Select(t => new TariffDto(
            t.Id,
            t.Name,
            t.Season?.Name,
            t.PassType?.Name,
            t.Price,
            t.PoolLimit
        ));

        return Ok(result);
    }
}

public record TariffDto(
    int Id,
    string Name,
    string? Season,
    string? PassType,
    decimal? Price,
    int? PoolLimit
);

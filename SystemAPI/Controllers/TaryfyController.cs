using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

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
            t.RideCount,
            t.PoolLimit
        ));

        return Ok(result);
    }

    // POST /api/taryfy
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] TariffModifyRequest req)
    {
        var seasonId = await db.DictSeasons
            .Where(s => s.Name == req.Season)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        var passTypeId = await db.DictPassTypes
            .Where(p => p.Name == req.PassType)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        var tariff = new Tariff
        {
            Name = req.Name,
            Price = req.Price,
            PoolLimit = req.PoolLimit,
            SeasonId = seasonId,
            PassTypeId = passTypeId
        };

        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        return Ok(new { id = tariff.Id });
    }

    // PUT /api/taryfy/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] TariffModifyRequest req)
    {
        var tariff = await db.Tariffs.FindAsync(id);
        if (tariff == null) return NotFound();

        var seasonId = await db.DictSeasons
            .Where(s => s.Name == req.Season)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        var passTypeId = await db.DictPassTypes
            .Where(p => p.Name == req.PassType)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        tariff.Name = req.Name;
        tariff.Price = req.Price;
        tariff.PoolLimit = req.PoolLimit;
        
        if (req.Season != null) tariff.SeasonId = seasonId;
        if (req.PassType != null) tariff.PassTypeId = passTypeId;

        await db.SaveChangesAsync();

        return Ok();
    }

    // DELETE /api/taryfy/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var tariff = await db.Tariffs.Include(t => t.SkiPasses).FirstOrDefaultAsync(t => t.Id == id);
        if (tariff == null) return NotFound();

        if (tariff.SkiPasses.Any())
        {
            return BadRequest("Nie można usunąć taryfy, ponieważ ma przypisane karnety.");
        }

        db.Tariffs.Remove(tariff);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

public record TariffDto(
    int Id,
    string Name,
    string? Season,
    string? PassType,
    decimal? Price,
    int? RideCount,
    int? PoolLimit
);

public record TariffModifyRequest(
    string Name,
    string? Season,
    string? PassType,
    decimal? Price,
    int? PoolLimit
);

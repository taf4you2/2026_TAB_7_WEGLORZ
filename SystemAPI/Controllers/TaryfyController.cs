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
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = db.Tariffs
            .Include(t => t.Season)
            .Include(t => t.PassType)
            .AsQueryable();

        if (!User.IsInRole("admin"))
            query = query.Where(t => t.IsActive == true || t.IsActive == null);

        var tariffs = await query
            .OrderBy(t => t.PassTypeId)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var result = tariffs.Select(t => new TariffDto(
            t.Id,
            t.Name,
            t.Season?.Name,
            t.PassType?.Name,
            t.Price,
            t.RideCount,
            t.PoolLimit,
            t.DiscountType,
            t.IsActive ?? true
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
            PassTypeId = passTypeId,
            DiscountType = req.DiscountType,
            IsActive = true
        };

        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync();

        return Ok(new { id = tariff.Id });
    }

    // POST /api/taryfy/bulk
    [HttpPost("bulk")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> CreateBulk([FromBody] TariffBulkCreateRequest req)
    {
        var seasonId = await db.DictSeasons
            .Where(s => s.Name == req.Season)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        var passTypeId = await db.DictPassTypes
            .Where(p => p.Name == req.PassType)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        var newTariffs = new List<Tariff>();
        foreach (var item in req.Discounts)
        {
            newTariffs.Add(new Tariff
            {
                Name = req.Name,
                DiscountType = item.DiscountType,
                Price = item.Price,
                PoolLimit = req.PoolLimit,
                SeasonId = seasonId,
                PassTypeId = passTypeId,
                IsActive = true
            });
        }

        db.Tariffs.AddRange(newTariffs);
        await db.SaveChangesAsync();

        return Ok();
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
        tariff.DiscountType = req.DiscountType;
        if (req.IsActive.HasValue) tariff.IsActive = req.IsActive.Value;

        if (req.Season != null) tariff.SeasonId = seasonId;
        if (req.PassType != null) tariff.PassTypeId = passTypeId;

        await db.SaveChangesAsync();

        return Ok();
    }

    // DELETE /api/taryfy/{id} - dezaktywacja bez usuwania historii.
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var tariff = await db.Tariffs.FirstOrDefaultAsync(t => t.Id == id);
        if (tariff == null) return NotFound();

        tariff.IsActive = false;
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
    int? PoolLimit,
    string? DiscountType,
    bool IsActive
);

public record TariffModifyRequest(
    string Name,
    string? Season,
    string? PassType,
    decimal? Price,
    int? PoolLimit,
    string? DiscountType,
    bool? IsActive = null
);

public record TariffBulkCreateRequest(
    string Name,
    string? Season,
    string? PassType,
    int? PoolLimit,
    List<DiscountPriceDto> Discounts
);

public record DiscountPriceDto(
    string DiscountType,
    decimal Price
);

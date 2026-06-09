using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/trasy")]
public class TrasyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/trasy
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = db.Trails
            .Include(t => t.Difficulty)
            .AsQueryable();

        if (!User.IsInRole("admin"))
            query = query.Where(t => t.IsActive == true || t.IsActive == null);

        var trails = await query
            .OrderBy(t => t.Name)
            .ToListAsync();

        var result = trails.Select(t => new TrailDto(
            t.Id,
            t.Name,
            t.Location,
            t.Length,
            t.Difficulty?.Name,
            t.IsActive ?? true
        ));

        return Ok(result);
    }

    // GET /api/trasy/trudnosci
    [HttpGet("trudnosci")]
    public async Task<IActionResult> GetDifficulties()
    {
        var difficulties = await db.DictTrailDifficulties
            .OrderBy(d => d.Id)
            .ToListAsync();
        return Ok(difficulties);
    }

    // POST /api/trasy
    [HttpPost]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Create([FromBody] TrailModifyRequest req)
    {
        var difficultyId = await db.DictTrailDifficulties
            .Where(d => d.Name == req.Difficulty)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();

        var trail = new Trail
        {
            Name = req.Name,
            Location = req.Location,
            Length = req.Length,
            DifficultyId = difficultyId,
            IsActive = true
        };

        db.Trails.Add(trail);
        await db.SaveChangesAsync();

        return Ok(new { id = trail.Id });
    }

    // PUT /api/trasy/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Update(int id, [FromBody] TrailModifyRequest req)
    {
        var trail = await db.Trails.FindAsync(id);
        if (trail == null) return NotFound();

        var difficultyId = await db.DictTrailDifficulties
            .Where(d => d.Name == req.Difficulty)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();

        trail.Name = req.Name;
        trail.Location = req.Location;
        trail.Length = req.Length;
        trail.DifficultyId = difficultyId;
        if (req.IsActive.HasValue) trail.IsActive = req.IsActive.Value;

        await db.SaveChangesAsync();

        return Ok();
    }

    // DELETE /api/trasy/{id} - dezaktywacja bez usuwania historii.
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Delete(int id)
    {
        var trail = await db.Trails
            .Include(t => t.Schedules)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trail == null) return NotFound();

        trail.IsActive = false;
        foreach (var schedule in trail.Schedules)
        {
            schedule.IsOpen = false;
            schedule.ClosureReason ??= "Trasa dezaktywowana";
            schedule.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        return NoContent();
    }
}

public record TrailDto(
    int Id,
    string Name,
    string? Location,
    decimal? Length,
    string? Difficulty,
    bool IsActive
);

public record TrailModifyRequest(
    string Name,
    string? Location,
    decimal? Length,
    string? Difficulty,
    bool? IsActive = null
);

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
        var trasy = await db.Trails
            .Include(t => t.Difficulty)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var result = trasy.Select(t => new TrailDto(
            t.Id,
            t.Name,
            t.Location,
            t.Length,
            t.Difficulty?.Name
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
            DifficultyId = difficultyId
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

        await db.SaveChangesAsync();

        return Ok();
    }

    // DELETE /api/trasy/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Delete(int id)
    {
        var trail = await db.Trails
            .Include(t => t.LiftTrails)
            .Include(t => t.Schedules)
            .FirstOrDefaultAsync(t => t.Id == id);
            
        if (trail == null) return NotFound();

        // Usuwamy powiązania przed usunięciem trasy
        db.LiftTrails.RemoveRange(trail.LiftTrails);
        db.TrailSchedules.RemoveRange(trail.Schedules);
        db.Trails.Remove(trail);
        
        await db.SaveChangesAsync();

        return NoContent();
    }
}

public record TrailDto(
    int Id,
    string Name,
    string? Location,
    decimal? Length,
    string? Difficulty
);

public record TrailModifyRequest(
    string Name,
    string? Location,
    decimal? Length,
    string? Difficulty
);

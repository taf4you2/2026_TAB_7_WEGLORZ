using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/bramki")]
public class BramkiController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/bramki
    [HttpGet]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> GetAll()
    {
        var gates = await db.Gates
            .Include(g => g.Lift)
            .OrderBy(g => g.LiftId)
            .ThenBy(g => g.Name)
            .ToListAsync();

        var result = gates.Select(g => new GateDto(
            g.Id,
            g.Name,
            g.IsActive ?? false,
            g.LiftId,
            g.Lift?.Name
        ));

        return Ok(result);
    }

    // POST /api/bramki
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] GateModifyRequest req)
    {
        var lift = await db.Lifts.FindAsync(req.LiftId);
        if (lift == null) return BadRequest("Podany wyciąg nie istnieje.");

        var gate = new Gate
        {
            Name = req.Name,
            LiftId = req.LiftId,
            IsActive = req.IsActive
        };

        db.Gates.Add(gate);
        await db.SaveChangesAsync();

        return Ok(new { id = gate.Id });
    }

    // PUT /api/bramki/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] GateModifyRequest req)
    {
        var gate = await db.Gates.FindAsync(id);
        if (gate == null) return NotFound();

        var lift = await db.Lifts.FindAsync(req.LiftId);
        if (lift == null) return BadRequest("Podany wyciąg nie istnieje.");

        gate.Name = req.Name;
        gate.LiftId = req.LiftId;
        gate.IsActive = req.IsActive;

        await db.SaveChangesAsync();

        return Ok();
    }

    // DELETE /api/bramki/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var gate = await db.Gates.Include(g => g.GateScans).FirstOrDefaultAsync(g => g.Id == id);
        if (gate == null) return NotFound();

        if (gate.GateScans.Any())
        {
            return BadRequest("Nie można usunąć bramki, która ma przypisane skanowania kart.");
        }

        db.Gates.Remove(gate);
        await db.SaveChangesAsync();

        return NoContent();
    }
}

public record GateDto(
    int Id,
    string? Name,
    bool IsActive,
    int? LiftId,
    string? LiftName
);

public record GateModifyRequest(
    string Name,
    int LiftId,
    bool IsActive
);

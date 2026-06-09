using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/wyciagi")]
public class WyciagController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/wyciagi
    // Publiczny endpoint — zwraca listę wyciągów z dzisiejszym rozkładem i statusem.
    // Używany przez portal narciarza (UC13) i dashboard kasjera.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var today = (int)DateTime.UtcNow.DayOfWeek; // 0=Sunday … 6=Saturday
        var now = DateTime.UtcNow.TimeOfDay;

        var lifts = await db.Lifts
            .Include(l => l.Schedules)
            .Include(l => l.LiftTrails)
                .ThenInclude(lt => lt.Trail)
                    .ThenInclude(t => t!.Difficulty)
            .Include(l => l.Gates)
            .Where(l => l.IsActive == true || l.IsActive == null)
            .OrderBy(l => l.Name)
            .ToListAsync();

        var result = lifts.Select(l =>
        {
            var todaySchedule = l.Schedules.FirstOrDefault(s => s.DayOfWeek == today);
            var opens = todaySchedule?.OpeningTime;
            var closes = todaySchedule?.ClosingTime;

            // Status na podstawie rozkładu dnia: czynny jeśli jesteśmy w oknie godzin pracy
            string status;
            if (opens == null || closes == null)
                status = "nieczynny";
            else if (l.IsActive == false)
                status = "nieaktywny";
            else if (now < opens)
                status = "przed_otwarciem";
            else if (now > closes)
                status = "po_zamknieciu";
            else
                status = "czynny";

            var trails = l.LiftTrails
                .Where(lt => lt.Trail == null || lt.Trail.IsActive == true || lt.Trail.IsActive == null)
                .Select(lt => new TrailSummaryDto(
                lt.Trail?.Id ?? 0,
                lt.Trail?.Name ?? "",
                lt.Trail?.Difficulty?.Name
            )).ToArray();

            return new LiftDto(
                l.Id,
                l.Name,
                status,
                opens,
                closes,
                trails,
                l.IsActive ?? true
            );
        });

        return Ok(result);
    }

    // POST /api/wyciagi
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] LiftModifyRequest req)
    {
        var lift = new Lift { Name = req.Name, IsActive = req.Status != "zamkniety" && req.Status != "awaria" };
        db.Lifts.Add(lift);
        await db.SaveChangesAsync();

        // Ustaw domyślny rozkład dla wszystkich dni tygodnia
        for (int i = 0; i <= 6; i++)
        {
            db.LiftSchedules.Add(new LiftSchedule
            {
                LiftId = lift.Id,
                DayOfWeek = i,
                OpeningTime = req.OpensAt,
                ClosingTime = req.ClosesAt
            });
        }
        await db.SaveChangesAsync();

        return Ok(new { id = lift.Id });
    }

    // PUT /api/wyciagi/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] LiftModifyRequest req)
    {
        var lift = await db.Lifts.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == id);
        if (lift == null) return NotFound();

        lift.Name = req.Name;
        lift.IsActive = req.Status != "zamkniety" && req.Status != "awaria";

        // Aktualizuj lub dodaj rozkład dla wszystkich dni
        for (int i = 0; i <= 6; i++)
        {
            var sched = lift.Schedules.FirstOrDefault(s => s.DayOfWeek == i);
            if (sched != null)
            {
                sched.OpeningTime = req.OpensAt;
                sched.ClosingTime = req.ClosesAt;
            }
            else
            {
                db.LiftSchedules.Add(new LiftSchedule
                {
                    LiftId = lift.Id,
                    DayOfWeek = i,
                    OpeningTime = req.OpensAt,
                    ClosingTime = req.ClosesAt
                });
            }
        }
        await db.SaveChangesAsync();

        return Ok();
    }

    // DELETE /api/wyciagi/{id} - dezaktywuje wyciag i jego bramki bez usuwania historii.
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var lift = await db.Lifts.Include(l => l.Schedules).Include(l => l.Gates).Include(l => l.LiftTrails).FirstOrDefaultAsync(l => l.Id == id);
        if (lift == null) return NotFound();

        lift.IsActive = false;
        foreach (var gate in lift.Gates)
            gate.IsActive = false;
        await db.SaveChangesAsync();

        return NoContent();
    }
}

public record LiftDto(
    int Id,
    string Name,
    string Status,
    TimeSpan? OpensAt,
    TimeSpan? ClosesAt,
    TrailSummaryDto[] Trails,
    bool IsActive
);

public record TrailSummaryDto(int Id, string Name, string? Difficulty);

public record LiftModifyRequest(
    string Name,
    string Status,
    TimeSpan? OpensAt,
    TimeSpan? ClosesAt
);

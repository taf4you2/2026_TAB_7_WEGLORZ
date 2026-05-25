using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

using Microsoft.AspNetCore.SignalR;
using SystemAPI.Hubs;

namespace SystemAPI.Controllers;

[ApiController]
[Route("api/wyciagi")]
public class WyciagController(SkiResortDbContext db, IHubContext<InfrastructureHub> hub) : ControllerBase
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
            .Include(l => l.Status)
            .OrderBy(l => l.Name)
            .ToListAsync();

        var result = lifts.Select(l =>
        {
            var todaySchedule = l.Schedules.FirstOrDefault(s => s.DayOfWeek == today);
            var opens = todaySchedule?.OpeningTime;
            var closes = todaySchedule?.ClosingTime;

            // Status na podstawie słownika, a jeśli brak, to z rozkładu dnia
            string status;
            if (l.Status != null)
                status = l.Status.Name;
            else if (opens == null || closes == null)
                status = "nieczynny";
            else if (now < opens)
                status = "przed_otwarciem";
            else if (now > closes)
                status = "po_zamknieciu";
            else
                status = "czynny";

            var trails = l.LiftTrails.Select(lt => new TrailSummaryDto(
                lt.Trail?.Id ?? 0,
                lt.Trail?.Name ?? "",
                lt.Trail?.Difficulty?.Name
            )).ToArray();

            return new LiftDto(
                l.Id,
                l.Name,
                status,
                l.Capacity,
                l.Type,
                opens,
                closes,
                trails
            );
        });

        return Ok(result);
    }

    // POST /api/wyciagi
    [HttpPost]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Create([FromBody] LiftModifyRequest req)
    {
        var lift = new Lift { 
            Name = req.Name,
            Capacity = req.Capacity,
            Type = req.Type 
        };
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
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Update(int id, [FromBody] LiftModifyRequest req)
    {
        var lift = await db.Lifts.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == id);
        if (lift == null) return NotFound();

        lift.Name = req.Name;
        lift.Capacity = req.Capacity;
        lift.Type = req.Type;

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

    // DELETE /api/wyciagi/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> Delete(int id)
    {
        var lift = await db.Lifts.Include(l => l.Schedules).Include(l => l.Gates).Include(l => l.LiftTrails).FirstOrDefaultAsync(l => l.Id == id);
        if (lift == null) return NotFound();

        db.LiftSchedules.RemoveRange(lift.Schedules);
        db.Gates.RemoveRange(lift.Gates);
        db.LiftTrails.RemoveRange(lift.LiftTrails);
        db.Lifts.Remove(lift);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // PUT /api/wyciagi/{id}/status
    [HttpPut("{id}/status")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLiftStatusRequest req)
    {
        var lift = await db.Lifts.FindAsync(id);
        if (lift == null) return NotFound();

        // 1=Otwarty, 2=Zamknięty, 3=Awaria, 4=Przerwa - tak będą ułożone ID z SQL inserta
        lift.StatusId = req.StatusId;
        await db.SaveChangesAsync();

        // Informowanie klientów via SignalR
        await hub.Clients.All.SendAsync("LiftStatusUpdated", id, req.StatusId);

        return Ok();
    }

    // PUT /api/wyciagi/{id}/trasy
    [HttpPut("{id}/trasy")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> UpdateTrails(int id, [FromBody] int[] trailIds)
    {
        var lift = await db.Lifts.Include(l => l.LiftTrails).FirstOrDefaultAsync(l => l.Id == id);
        if (lift == null) return NotFound();

        db.LiftTrails.RemoveRange(lift.LiftTrails);
        foreach (var tId in trailIds)
        {
            db.LiftTrails.Add(new LiftTrail { LiftId = id, TrailId = tId });
        }
        await db.SaveChangesAsync();
        return Ok();
    }

    // GET /api/wyciagi/{id}/harmonogram
    [HttpGet("{id}/harmonogram")]
    public async Task<IActionResult> GetSchedules(int id)
    {
        var schedules = await db.LiftSchedules
            .Where(s => s.LiftId == id)
            .Select(s => new {
                s.Id,
                s.DayOfWeek,
                s.OpeningTime,
                s.ClosingTime,
                s.SeasonId
            })
            .ToListAsync();
        return Ok(schedules);
    }

    // PUT /api/wyciagi/{id}/harmonogram
    [HttpPut("{id}/harmonogram")]
    [Authorize(Roles = "admin,kasjer")]
    public async Task<IActionResult> UpdateSchedules(int id, [FromBody] LiftScheduleRequest[] req)
    {
        var lift = await db.Lifts.Include(l => l.Schedules).FirstOrDefaultAsync(l => l.Id == id);
        if (lift == null) return NotFound();

        db.LiftSchedules.RemoveRange(lift.Schedules);
        foreach (var s in req)
        {
            db.LiftSchedules.Add(new LiftSchedule {
                LiftId = id,
                DayOfWeek = s.DayOfWeek,
                OpeningTime = s.OpeningTime,
                ClosingTime = s.ClosingTime,
                SeasonId = s.SeasonId
            });
        }
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record LiftDto(
    int Id,
    string Name,
    string Status,
    int? Capacity,
    string? Type,
    TimeSpan? OpensAt,
    TimeSpan? ClosesAt,
    TrailSummaryDto[] Trails
);

public record TrailSummaryDto(int Id, string Name, string? Difficulty);

public record LiftModifyRequest(
    string Name,
    string Status,
    int? Capacity,
    string? Type,
    TimeSpan? OpensAt,
    TimeSpan? ClosesAt
);

public record UpdateLiftStatusRequest(int? StatusId);

public record LiftScheduleRequest(int? DayOfWeek, TimeSpan? OpeningTime, TimeSpan? ClosingTime, int? SeasonId);

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

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
                opens,
                closes,
                trails
            );
        });

        return Ok(result);
    }
}

public record LiftDto(
    int Id,
    string Name,
    string Status,
    TimeSpan? OpensAt,
    TimeSpan? ClosesAt,
    TrailSummaryDto[] Trails
);

public record TrailSummaryDto(int Id, string Name, string? Difficulty);

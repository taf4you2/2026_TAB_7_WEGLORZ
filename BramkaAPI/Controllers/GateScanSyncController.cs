using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;
using SystemStacjiNarciarskiejDLL.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BramkaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GateScanSyncController : ControllerBase
{
    private readonly SkiResortDbContext _db;

    public GateScanSyncController(SkiResortDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Sync([FromBody] List<GateScanSyncDto> syncRequests)
    {
        if (syncRequests == null || !syncRequests.Any())
        {
            return BadRequest("Lista odbić jest pusta.");
        }

        var scansToInsert = syncRequests.Select(req => new GateScan
        {
            CardId = req.CardId,
            GateId = req.GateId ?? 1,
            ScanTime = req.ScanTime ?? DateTime.Now,
            TimeBlockedUntil = req.TimeBlockedUntil,
            VerificationResultId = req.VerificationResultId ?? 1,
            PassTypeId = req.PassTypeId
        }).ToList();

        _db.GateScans.AddRange(scansToInsert);
        
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Zsynchronizowano {scansToInsert.Count} odbić z bramki." });
    }
}

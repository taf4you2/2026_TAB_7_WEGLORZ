using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;
using SystemStacjiNarciarskiejDLL.Models.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BramkaAPI.Controllers;

[ApiController]
[Route("api/bramka/[controller]")]
public class ScanController : ControllerBase
{
    private readonly SkiResortDbContext _db;
    
    private const int VERIFICATION_ACCEPTED = 1;
    private const int VERIFICATION_REJECTED = 2;
    private const int CARD_STATUS_ACTIVE = 1;
    private const int PASS_STATUS_ACTIVE = 1;
    
    public ScanController(SkiResortDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Scan([FromBody] GateScanRequestDto req)
    {
        var scanTime = DateTime.Now;
        var response = new GateScanResponseDto { IsGranted = false };
        int verificationResult = VERIFICATION_REJECTED;
        int? usedPassTypeId = null;

        var gate = await _db.Gates
            .Include(g => g.Lift)
            .FirstOrDefaultAsync(g => g.Id == req.GateId);

        if (gate == null || gate.IsActive == false)
        {
            response.Message = "Bramka jest nieaktywna. Odmowa dostepu.";
            response.ReasonCode = "GATE_INACTIVE";
            await LogScan(req, scanTime, verificationResult, usedPassTypeId, null);
            return Ok(response);
        }

        if (gate.Lift?.IsActive == false)
        {
            response.Message = "Wyciag jest nieaktywny. Odmowa dostepu.";
            response.ReasonCode = "LIFT_INACTIVE";
            await LogScan(req, scanTime, verificationResult, usedPassTypeId, null);
            return Ok(response);
        }

        var card = await _db.Cards
            .Include(c => c.SkiPasses)
            .ThenInclude(sp => sp.Tariff)
            .FirstOrDefaultAsync(c => c.Id == req.CardId);

        if (card == null)
        {
            response.Message = "Nieznana karta";
            response.ReasonCode = "UNKNOWN_CARD";
            await LogScan(req, scanTime, verificationResult, usedPassTypeId, null);
            return Ok(response);
        }

        if (card.StatusId == 3) // 3 = zastrzezony (zablokowany)
        {
            response.Message = "Karta jest zastrzeżona. Odmowa dostępu.";
            response.ReasonCode = "CARD_BLOCKED";
            await LogScan(req, scanTime, verificationResult, usedPassTypeId, null);
            return Ok(response);
        }

        var lastScan = await _db.GateScans
            .Where(gs => gs.CardId == req.CardId)
            .OrderByDescending(gs => gs.ScanTime)
            .FirstOrDefaultAsync();

        if (lastScan != null && lastScan.TimeBlockedUntil.HasValue && lastScan.TimeBlockedUntil.Value > scanTime)
        {
            response.Message = "Anti-passback: Musisz odczekać przed kolejnym przejściem";
            response.ReasonCode = "ANTI_PASSBACK";
            await LogScan(req, scanTime, verificationResult, usedPassTypeId, null);
            return Ok(response);
        }

        // Weryfikacja karnetu
        var validPass = card.SkiPasses
            .Where(sp => sp.StatusId == PASS_STATUS_ACTIVE)
            .Where(sp => sp.ValidFrom == null || sp.ValidFrom <= scanTime)
            .Where(sp => sp.ValidTo == null || sp.ValidTo >= scanTime)
            .Where(sp => sp.RemainingRides == null || sp.RemainingRides > 0)
            .OrderByDescending(sp => sp.RemainingRides != null)
            .FirstOrDefault();

        if (validPass == null)
        {
            response.Message = "Brak ważnego karnetu";
            response.ReasonCode = "NO_VALID_PASS";
            await LogScan(req, scanTime, verificationResult, usedPassTypeId, null);
            return Ok(response);
        }

        // Zezwolenie na przejście
        verificationResult = VERIFICATION_ACCEPTED;
        usedPassTypeId = validPass.Tariff?.PassTypeId;
        
        // Czas blokady, np. 5 minut
        var blockedUntil = scanTime.AddMinutes(5);

        // Odjęcie punktów, jeśli dotyczy
        if (validPass.RemainingRides.HasValue)
        {
            validPass.RemainingRides -= 1;
        }

        response.IsGranted = true;
        response.Message = "Brama otwarta";
        response.ReasonCode = "OK";

        await LogScan(req, scanTime, verificationResult, usedPassTypeId, blockedUntil);

        return Ok(response);
    }

    private async Task LogScan(GateScanRequestDto req, DateTime scanTime, int resultId, int? passTypeId, DateTime? blockedUntil)
    {
        var scan = new GateScan
        {
            CardId = req.CardId,
            GateId = req.GateId,
            ScanTime = scanTime,
            VerificationResultId = resultId,
            PassTypeId = passTypeId,
            TimeBlockedUntil = blockedUntil
        };

        _db.GateScans.Add(scan);
        await _db.SaveChangesAsync();
    }
}

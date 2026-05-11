using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace SystemAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/zwroty")]
public class ZwrotyController(SkiResortDbContext db) : ControllerBase
{
    // GET /api/zwroty/oczekujace
    // Lista wniosków o zwrot czekających na obsługę kasjera.
    // Zwraca karnety ze statusem "oczekuje_na_zwrot" wraz z szacunkową kwotą zwrotu.
    [HttpGet("oczekujace")]
    public async Task<IActionResult> GetPending()
    {
        // TODO: ustalić właściwą nazwę statusu "oczekuje_na_zwrot" w DictPassStatus
        var pending = await db.SkiPasses
            .Include(sp => sp.Status)
            .Include(sp => sp.Tariff)
            .Include(sp => sp.Card)
            .Include(sp => sp.Reservation)
                .ThenInclude(r => r!.User)
            .Where(sp => sp.Status != null && sp.Status.Name == "oczekuje_na_zwrot")
            .OrderBy(sp => sp.ValidTo)
            .ToListAsync();

        const decimal ManipulationFee = 10m;

        var result = pending.Select(sp =>
        {
            var totalDays = sp.ValidFrom.HasValue && sp.ValidTo.HasValue
                ? Math.Max(1, (int)(sp.ValidTo.Value - sp.ValidFrom.Value).TotalDays)
                : 1;
            var usedDays = sp.ValidFrom.HasValue
                ? Math.Max(0, (int)(DateTime.UtcNow - sp.ValidFrom.Value).TotalDays)
                : 0;
            var remainingDays = Math.Max(0, totalDays - usedDays);
            var pricePerDay = (sp.Tariff?.Price ?? 0) / totalDays;
            var estimatedRefund = Math.Max(0, pricePerDay * remainingDays - ManipulationFee);

            return new PendingReturnDto(
                PassId: sp.Id,
                CardRfid: sp.CardId ?? "",
                OwnerEmail: sp.Reservation?.User?.Email,
                PassType: sp.Tariff?.Name,
                ValidTo: sp.ValidTo,
                RemainingDays: remainingDays,
                EstimatedRefund: estimatedRefund
            );
        });

        return Ok(result);
    }

    [HttpPost("{id}/zatwierdz")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ApproveReturn(int id, [FromBody] ApproveReturnRequest req)
    {
        var pass = await db.SkiPasses.Include(sp => sp.Tariff).FirstOrDefaultAsync(sp => sp.Id == id);
        if (pass == null) return NotFound();

        var returnedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "zwrocony");
        pass.StatusId = returnedStatus?.Id;

        if (pass.CardId != null && req.ReturnCard)
        {
            var wolnaId = await db.DictCardStatuses
                .Where(s => s.Name == "wolna")
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
            if (wolnaId.HasValue)
                await db.Cards.Where(c => c.Id == pass.CardId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.StatusId, wolnaId.Value));
        }

        var opType = await db.DictOperationTypes.FirstOrDefaultAsync(o => o.Name == "zwrot_karnetu");
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var transaction = new Transaction
        {
            ReservationId = pass.ReservationId,
            CashierId = adminId,
            OperationTypeId = opType?.Id,
            Amount = -req.RefundAmount,
            TransactionDate = DateTime.UtcNow
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{id}/odrzuc")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RejectReturn(int id)
    {
        var pass = await db.SkiPasses.FindAsync(id);
        if (pass == null) return NotFound();

        var rejectedStatus = await db.DictPassStatuses.FirstOrDefaultAsync(s => s.Name == "aktywny"); // Lub odrzucony_zwrot
        pass.StatusId = rejectedStatus?.Id;

        await db.SaveChangesAsync();
        return Ok();
    }
}

public record ApproveReturnRequest(decimal RefundAmount, bool ReturnCard);

public record PendingReturnDto(
    int PassId,
    string CardRfid,
    string? OwnerEmail,
    string? PassType,
    DateTime? ValidTo,
    int RemainingDays,
    decimal EstimatedRefund
);

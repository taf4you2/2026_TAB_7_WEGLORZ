using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

namespace SystemAPI.Controllers;

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
}

public record PendingReturnDto(
    int PassId,
    string CardRfid,
    string? OwnerEmail,
    string? PassType,
    DateTime? ValidTo,
    int RemainingDays,
    decimal EstimatedRefund
);

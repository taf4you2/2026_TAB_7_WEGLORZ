using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using SystemStacjiNarciarskiejDLL;
using SystemStacjiNarciarskiejDLL.Models;

namespace BramkaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BramkaController : ControllerBase
    {
        private readonly SkiResortDbContext _context;

        public BramkaController(SkiResortDbContext context)
        {
            _context = context;
        }

        [HttpGet("weryfikuj")]
        public IActionResult weryfikujDostęp(int a, int b)
        {
            if ((a == 0 || a == 1) && (b == 0 || b == 1))
            {
                int wynik = a & b;

                return Ok(new { Wynik = wynik, Wiadomosc = $"Wynik operacji AND dla {a} i {b} to: {wynik}" });
            }
            return BadRequest("Podano nie prawidołowe wartości");
        }

        [HttpGet("karty")]
        public async Task<IActionResult> PobierzKarty()
        {
            var karty = await _context.Cards
                .Select(c => new
                {
                    c.Id,
                    c.StatusId,
                    c.PhysicalCondition,
                    c.AddedToPoolAt
                })
                .ToListAsync();

            return Ok(karty);
        }

        [HttpGet("sprawdz-karte/{id}")]
        public async Task<IActionResult> SprawdzKarte(string id)
        {
            var karta = await _context.Cards
                .Include(c => c.Status)
                .Include(c => c.SkiPasses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (karta == null)
            {
                return NotFound(new { Wiadomosc = "Karta o podanym ID nie istnieje." });
            }

            if (karta.Status?.Name == "zablokowana")
            {
                return Ok(new { Aktywna = false, Wiadomosc = "Karta jest zablokowana. Odmowa dostępu." });
            }

            var now = DateTime.UtcNow;
            bool hasValidPass = karta.SkiPasses.Any(sp => sp.ValidFrom <= now && sp.ValidTo >= now && sp.StatusId == 1); // 1 = aktywny wg DB

            if (hasValidPass)
            {
                return Ok(new { Aktywna = true, Wiadomosc = "Karta posiada ważny karnet. Dostęp przyznany." });
            }

            return Ok(new { Aktywna = false, Wiadomosc = "Karta nie posiada ważnego karnetu. Odmowa dostępu." });
        }

        [HttpGet("aktywne-karty")]
        public async Task<IActionResult> PobierzAktywneKarty()
        {
            var now = DateTime.UtcNow;
            var karty = await _context.Cards
                .Include(c => c.Status)
                .Include(c => c.SkiPasses)
                .Where(c => c.Status != null && c.Status.Name != "zablokowana" && c.SkiPasses.Any(sp => sp.ValidFrom <= now && sp.ValidTo >= now && sp.StatusId == 1))
                .Select(c => new
                {
                    Id = c.Id,
                    CzyAktywna = true,
                    WaznaDo = c.SkiPasses.Where(sp => sp.ValidFrom <= now && sp.ValidTo >= now && sp.StatusId == 1).Max(sp => sp.ValidTo)
                })
                .ToListAsync();

            return Ok(karty);
        }
    }
}

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
            var karta = await _context.Cards.FirstOrDefaultAsync(c => c.Id == id);

            if (karta == null)
            {
                return NotFound(new { Wiadomosc = "Karta o podanym ID nie istnieje." });
            }

            if (karta.StatusId == 1)
            {
                return Ok(new { Aktywna = true, Wiadomosc = "Karta jest aktywna. Dostęp przyznany." });
            }

            return Ok(new { Aktywna = false, Wiadomosc = "Karta jest nieaktywna. Odmowa dostępu." });
        }
    }
}

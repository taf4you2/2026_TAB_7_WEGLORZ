using Microsoft.AspNetCore.Mvc;
using StacjaNarciarskaDB.Models;

namespace BramkaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BramkaController : ControllerBase
    {
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

        [HttpGet("test")]
        public IActionResult test()
        {
            Karnet testowyKarnet = new Karnet
            {
                Id = 1,
                DataWaznosciOd = DateTime.Now,
                DataWaznosciDo = DateTime.Now.AddDays(7),
                PowodBlokady = "brak"
            };

            return Ok(testowyKarnet);
        }

    }
}

using Microsoft.AspNetCore.Mvc;

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
                // Oblicza stan logiczny dla dostępu przez bramkę.
                int wynik = a & b;

                // Zwraca pozytywną odpowiedź wraz z wyliczonym wynikiem.
                return Ok(new { Wynik = wynik, Wiadomosc = $"Wynik operacji AND dla {a} i {b} to: {wynik}" });
            }
            return BadRequest("Podano nie prawidołowe wartości");
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bramka
{
    class Program
    {

        static readonly HttpClient client = new();

        static async Task Main(string[] args)
        {
            InicjalizujBazeLokalna();
            await SynchronizujBazeLokalnaAsync();
            UruchomSynchronizacjeWTle();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("\n");
                Console.Write("Podaj ID karty do sprawdzenia: ");

                string cardId = await PobierzWartoscZOdswiezaniemAsync();

                if (string.IsNullOrEmpty(cardId))
                    continue;

                Console.Clear();
                Console.WriteLine("\nSprawdzanie karty...");

                await WeryfikujKarteAsync(cardId);
                await CzekajPoWeryfikacjiAsync(5000);
            }
        }

        static void InicjalizujBazeLokalna()
        {
            using var db = new BramkaDbContext();
            db.Database.EnsureCreated();
        }

        static async Task WeryfikujKarteAsync(string cardId)
        {
            using var db = new BramkaDbContext();
            var karta = await db.KartyLokalne.FindAsync(cardId);

            Console.WriteLine();

            if (karta != null)
            {
                WyswietlStatusKarty(karta.CzyAktywna, false);
                return;
            }

            await SprawdzKarteWApiAsync(cardId, db);
        }

        static async Task SprawdzKarteWApiAsync(string cardId, BramkaDbContext db)
        {
            string url = $"http://localhost:49226/api/bramka/sprawdz-karte/{cardId}";

            try
            {
                Task<HttpResponseMessage> getTask = client.GetAsync(url);

                while (!getTask.IsCompleted)
                {
                    OdswiezNaglowek();
                    await Task.Delay(200);
                }

                HttpResponseMessage response = await getTask;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    JsonElement root = doc.RootElement;

                    bool czyAktywna = false;
                    if (root.TryGetProperty("aktywna", out JsonElement prop) || root.TryGetProperty("Aktywna", out prop))
                    {
                        czyAktywna = prop.GetBoolean();
                    }

                    db.KartyLokalne.Add(new KartaLokalna { Id = cardId, CzyAktywna = czyAktywna });
                    await db.SaveChangesAsync();

                    Console.WriteLine("[Serwer API] Karta zsynchronizowana pomyślnie.");
                    WyswietlStatusKarty(czyAktywna, true);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(">>> ODMÓWIONO DOSTĘPU (Karta nie istnieje w systemie) <<<");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"\nNieoczekiwany błąd serwera: {response.StatusCode}");
                }
            }
            catch (HttpRequestException e)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"\nBłąd połączenia z serwerem API: {e.Message}");
                Console.WriteLine("Bramka w trybie offline nie może zweryfikować nowej karty!");
                Console.ResetColor();
            }
        }

        static async Task CzekajPoWeryfikacjiAsync(int waitTime)
        {
            int elapsed = 0;
            int interval = 200;

            while (elapsed < waitTime)
            {
                OdswiezNaglowek();

                if (Console.KeyAvailable)
                {
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(intercept: true);
                    }
                    break;
                }

                await Task.Delay(interval);
                elapsed += interval;
            }
        }

        static void WyswietlStatusKarty(bool czyAktywna, bool serwer)
        {
            Console.WriteLine();
            if (czyAktywna)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($">>> ZAPRASZAMY [{(serwer ? "serwer" : "lokalnie")}] <<<");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(">>> ODMOWA DOSTĘPU <<<");
            }
            Console.ResetColor();
        }

        static async Task<string> PobierzWartoscZOdswiezaniemAsync()
        {
            string idKarty = "";

            while (true)
            {
                OdswiezNaglowek();

                while (Console.KeyAvailable)
                {
                    var klawisz = Console.ReadKey(intercept: true);

                    if (klawisz.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        return idKarty;
                    }
                    else if (klawisz.Key == ConsoleKey.Backspace)
                    {
                        if (idKarty.Length > 0)
                        {
                            idKarty = idKarty.Substring(0, idKarty.Length - 1);
                            Console.Write("\b \b");
                        }
                    }
                    else if (!char.IsControl(klawisz.KeyChar))
                    {
                        idKarty += klawisz.KeyChar;
                        Console.Write(klawisz.KeyChar);
                    }
                }

                await Task.Delay(200);
            }
        }

        static void OdswiezNaglowek()
        {
            int obecnaKolumna = Console.CursorLeft;
            int obecnyWiersz = Console.CursorTop;

            Console.SetCursorPosition(0, 0);
            string numerBramki = "1";

            string naglowek = $"=== BRAMKA {numerBramki} | {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n";
            Console.Write(naglowek);

            Console.SetCursorPosition(obecnaKolumna, obecnyWiersz);
        }

        static void UruchomSynchronizacjeWTle()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await SynchronizujBazeLokalnaAsync();
                }
            });
        }

        static async Task SynchronizujBazeLokalnaAsync()
        {
            try
            {
                using var db = new BramkaDbContext();

                var wygasleKarty = db.KartyLokalne
                    .Where(k => k.WaznaDo.HasValue && k.WaznaDo.Value < DateTime.Now)
                    .ToList();

                if (wygasleKarty.Any())
                {
                    db.KartyLokalne.RemoveRange(wygasleKarty);
                    await db.SaveChangesAsync();
                }

                string url = "http://localhost:49226/api/bramka/aktywne-karty";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    var opcje = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var aktywneKarty = JsonSerializer.Deserialize<List<KartaLokalna>>(jsonResponse, opcje);

                    if (aktywneKarty != null)
                    {
                        foreach (var kartaDto in aktywneKarty)
                        {
                            var istniejaca = await db.KartyLokalne.FindAsync(kartaDto.Id);

                            if (istniejaca == null)
                            {
                                db.KartyLokalne.Add(new KartaLokalna
                                {
                                    Id = kartaDto.Id,
                                    CzyAktywna = kartaDto.CzyAktywna,
                                    WaznaDo = kartaDto.WaznaDo
                                });
                            }
                            else
                            {
                                istniejaca.CzyAktywna = kartaDto.CzyAktywna;
                                istniejaca.WaznaDo = kartaDto.WaznaDo;
                            }
                        }
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex){ // zastanawiam sie jaka obsluge bledow dodac
            }
        }
    }
}
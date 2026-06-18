using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bramka
{
    class Program
    {

        static readonly HttpClient client = new();
        const string BramkaApiBaseUrl = "http://localhost:49226";
        const int GateId = 1;

        static async Task Main(string[] args)
        {
            InicjalizujBazeLokalna();
            await SynchronizujBazeLokalnaAsync();
            UruchomSynchronizacjeWTle();
            UruchomWysylkeOdbicWTle();

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

            Console.WriteLine();

            if (await ZarejestrujOdbicieWApiAsync(cardId))
            {
                return;
            }

            var karta = await db.KartyLokalne.FindAsync(cardId);

            if (karta != null)
            {
                await ZapiszOdbicieLokalneAsync(db, cardId, karta.CzyAktywna ? 1 : 2);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("[Offline] Zapisano odbicie lokalnie. Zostanie wyslane po odzyskaniu polaczenia.");
                Console.ResetColor();
                WyswietlStatusKarty(karta.CzyAktywna, false);
                return;
            }

            await ZapiszOdbicieLokalneAsync(db, cardId, 2);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[Offline] Brak polaczenia z API i karta nie jest w lokalnym cache.");
            Console.WriteLine("Zapisano odmowe lokalnie do pozniejszej synchronizacji.");
            Console.ResetColor();
            WyswietlStatusKarty(false, false);
        }

        static async Task<bool> ZarejestrujOdbicieWApiAsync(string cardId)
        {
            string url = $"{BramkaApiBaseUrl}/api/bramka/Scan";

            try
            {
                var payload = new
                {
                    cardId,
                    gateId = GateId
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                Task<HttpResponseMessage> postTask = client.PostAsync(url, content);

                while (!postTask.IsCompleted)
                {
                    OdswiezNaglowek();
                    await Task.Delay(200);
                }

                HttpResponseMessage response = await postTask;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"\nNieoczekiwany blad serwera przy rejestracji odbicia: {response.StatusCode}");
                    return false;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                bool isGranted = false;
                if (root.TryGetProperty("isGranted", out JsonElement grantedProp) || root.TryGetProperty("IsGranted", out grantedProp))
                {
                    isGranted = grantedProp.GetBoolean();
                }

                string message = "";
                if (root.TryGetProperty("message", out JsonElement messageProp) || root.TryGetProperty("Message", out messageProp))
                {
                    message = messageProp.GetString() ?? "";
                }

                Console.WriteLine("[Serwer API] Odbicie zarejestrowane w bazie danych.");
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine(message);
                }

                WyswietlStatusKarty(isGranted, true);
                return true;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"\nBlad polaczenia z serwerem API: {e.Message}");
                return false;
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine($"\nPrzekroczono czas oczekiwania na serwer API: {e.Message}");
                return false;
            }
        }

        static async Task ZapiszOdbicieLokalneAsync(BramkaDbContext db, string cardId, int verificationResultId)
        {
            db.OdbiciaLokalne.Add(new OdbicieLokalne
            {
                CardId = cardId,
                GateId = GateId,
                ScanTime = DateTime.Now,
                VerificationResultId = verificationResultId
            });

            await db.SaveChangesAsync();
        }

        static async Task SprawdzKarteWApiAsync(string cardId, BramkaDbContext db)
        {
            string url = $"{BramkaApiBaseUrl}/api/bramka/sprawdz-karte/{cardId}";

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

                    DateTime? waznaDo = null;
                    if (root.TryGetProperty("waznaDo", out JsonElement dateProp) || root.TryGetProperty("WaznaDo", out dateProp))
                    {
                        if (dateProp.ValueKind != JsonValueKind.Null)
                            waznaDo = dateProp.GetDateTime();
                    }

                if (czyAktywna)
                {
                    db.KartyLokalne.Add(new KartaLokalna { Id = cardId, CzyAktywna = czyAktywna, WaznaDo = waznaDo });
                }
                    
                    await ZapiszOdbicieLokalneAsync(db, cardId, czyAktywna ? 1 : 2);

                    Console.WriteLine("[Serwer API] Karta zweryfikowana pomyślnie.");
                    WyswietlStatusKarty(czyAktywna, true);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await ZapiszOdbicieLokalneAsync(db, cardId, 2);

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(">>> ODMÓWIONO DOSTĘPU (Karta nie istnieje w systemie) <<<");
                    Console.ResetColor();
                }
                else
                {
                    await ZapiszOdbicieLokalneAsync(db, cardId, 2);
                    Console.WriteLine($"\nNieoczekiwany błąd serwera: {response.StatusCode}");
                }
            }
            catch (HttpRequestException e)
            {
                await ZapiszOdbicieLokalneAsync(db, cardId, 2);

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

                string url = $"{BramkaApiBaseUrl}/api/bramka/aktywne-karty";
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
            catch (Exception){ // zastanawiam sie jaka obsluge bledow dodac
            }
        }

        static void UruchomWysylkeOdbicWTle()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await WyslijZalegleOdbiciaAsync();
                }
            });
        }

        static async Task WyslijZalegleOdbiciaAsync()
        {
            try
            {
                using var db = new BramkaDbContext();
                var nieZsynchronizowane = db.OdbiciaLokalne.ToList();

                if (!nieZsynchronizowane.Any())
                    return;

                var payload = nieZsynchronizowane.Select(o => new
                {
                    cardId = o.CardId,
                    gateId = o.GateId,
                    scanTime = o.ScanTime,
                    verificationResultId = o.VerificationResultId,
                    passTypeId = o.PassTypeId
                }).ToList();

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                string url = $"{BramkaApiBaseUrl}/api/GateScanSync";
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    db.OdbiciaLokalne.RemoveRange(nieZsynchronizowane);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                // Ignoruj błędy, w następnej pętli spróbuje ponownie
            }
        }
    }
}

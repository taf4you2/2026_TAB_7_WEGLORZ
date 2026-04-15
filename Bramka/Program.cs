using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bramka
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using HttpClient client = new HttpClient();

            while (true)
            {
                Console.Clear();

                Console.WriteLine("\n");
                Console.Write("Podaj ID karty do sprawdzenia: ");

                string cardId = await PobierzWartoscZOdswiezaniemAsync();

                if (!string.IsNullOrEmpty(cardId))
                {
                    Console.Clear();

                    Console.WriteLine("\nSprawdzanie karty...");

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

                            Console.WriteLine();
                            if (czyAktywna)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine(">>> ZAPRASZAMY <<<");
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine(">>> ODMÓWIONO DOSTĘPU <<<");
                            }
                            Console.ResetColor();
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(">>> ODMÓWIONO DOSTĘPU (Karta nie istnieje) <<<");
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
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine("Nieprawidłowy format ID.");
                }

                int elapsed = 0;
                int interval = 100;
                int waitTime = 5000;

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

            string naglowek = $"=== BRAMKA {numerBramki} | {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===";
            Console.Write(naglowek);

            Console.SetCursorPosition(obecnaKolumna, obecnyWiersz);
        }
    }
}
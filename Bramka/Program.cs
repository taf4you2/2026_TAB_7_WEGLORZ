using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Bramka
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Podaj wartość (0 lub 1):");
            int a = int.Parse(Console.ReadLine());
            Console.WriteLine("Podaj wartość (0 lub 1):");
            int b = int.Parse(Console.ReadLine());

            if ((a == 0 || a == 1) && (b == 0 || b == 1))
            {
                using HttpClient client = new HttpClient();

                string url = $"http://localhost:49226/api/bramka/weryfikuj?a={a}&b={b}";

                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);

                    response.EnsureSuccessStatusCode();

                    Console.WriteLine("\nOdpowiedź z serwera API:");

                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
                catch (HttpRequestException e)
                {
                    
                    Console.WriteLine($"\nBłąd połączenia z serwerem API: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("Nieprawidłowe wartośći");
            }
        }
    }
}
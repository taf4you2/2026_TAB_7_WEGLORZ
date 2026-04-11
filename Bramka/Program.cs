using System;

namespace Bramka
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Podaj pierwszą wartość (0 lub 1):");
            int a = int.Parse(Console.ReadLine());
            Console.WriteLine("Podaj drugą wartość (0 lub 1):");
            int b = int.Parse(Console.ReadLine());
            if ((a == 0 || a == 1) && (b == 0 || b == 1))
            {
                int wynik = a & b; // Operacja AND
                Console.WriteLine($"Wynik operacji AND dla {a} i {b} to: {wynik}");
            }
            else
            {
                Console.WriteLine("Nieprawidłowe wartości. Proszę podać 0 lub 1.");
            }
        }
    }
}
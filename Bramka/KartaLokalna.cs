using System;
using System.Collections.Generic;
using System.Text;

namespace Bramka
{
    public class KartaLokalna
    {
        public string Id { get; set; } = string.Empty;
        public bool CzyAktywna { get; set; }
        public DateTime? WaznaDo { get; set; }
    }
}

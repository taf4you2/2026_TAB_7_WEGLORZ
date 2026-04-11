using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Trasa
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public string? Lokalizacja { get; set; }

    public decimal? Dlugosc { get; set; }

    public int? TrudnoscId { get; set; }

    public int? PlanistaId { get; set; }

    public virtual ICollection<HarmonogramTrasy> HarmonogramTrasies { get; set; } = new List<HarmonogramTrasy>();

    public virtual PlanistaTra? Planista { get; set; }

    public virtual SlownikTrudnoscTrasy? Trudnosc { get; set; }

    public virtual ICollection<Wyciag> Wyciags { get; set; } = new List<Wyciag>();
}

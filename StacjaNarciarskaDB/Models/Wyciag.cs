using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Wyciag
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public string? Lokalizacja { get; set; }

    public decimal? Dlugosc { get; set; }

    public int? PlanistaId { get; set; }

    public virtual ICollection<Bramka> Bramkas { get; set; } = new List<Bramka>();

    public virtual ICollection<HarmonogramWyciagu> HarmonogramWyciagus { get; set; } = new List<HarmonogramWyciagu>();

    public virtual PlanistaTra? Planista { get; set; }

    public virtual ICollection<Trasa> Trasas { get; set; } = new List<Trasa>();
}

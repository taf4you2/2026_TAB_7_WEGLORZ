using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class RaportZmianowy
{
    public int Id { get; set; }

    public int? KasjerId { get; set; }

    public DateTime? DataRozpoczecia { get; set; }

    public DateTime? DataZakonczenia { get; set; }

    public decimal? SumaPrzychody { get; set; }

    public decimal? SumaZwrotyKaucji { get; set; }

    public int? LiczbaWydanychKart { get; set; }

    public virtual Kasjer? Kasjer { get; set; }
}

using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Transakcja
{
    public int Id { get; set; }

    public int? RezerwacjaId { get; set; }

    public int? KasjerId { get; set; }

    public int? TypOperacjiId { get; set; }

    public decimal Kwota { get; set; }

    public DateTime? DataTransakcji { get; set; }

    public virtual Kasjer? Kasjer { get; set; }

    public virtual Rezerwacja? Rezerwacja { get; set; }

    public virtual SlownikTypOperacji? TypOperacji { get; set; }
}

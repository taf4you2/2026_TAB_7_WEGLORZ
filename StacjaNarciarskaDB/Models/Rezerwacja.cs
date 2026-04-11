using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Rezerwacja
{
    public int Id { get; set; }

    public string NumerRezerwacji { get; set; } = null!;

    public int? UzytkownikId { get; set; }

    public DateTime? DataRezerwacji { get; set; }

    public int? StatusId { get; set; }

    public virtual ICollection<Karnet> Karnets { get; set; } = new List<Karnet>();

    public virtual SlownikStatusRezerwacji? Status { get; set; }

    public virtual ICollection<Transakcja> Transakcjas { get; set; } = new List<Transakcja>();

    public virtual Uzytkownik? Uzytkownik { get; set; }
}

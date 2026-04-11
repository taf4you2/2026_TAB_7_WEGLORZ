using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Taryfa
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public int? SezonId { get; set; }

    public int? RodzajKarnetuId { get; set; }

    public decimal? Cena { get; set; }

    public int? LimitPuli { get; set; }

    public virtual ICollection<Karnet> Karnets { get; set; } = new List<Karnet>();

    public virtual SlownikRodzajKarnetu? RodzajKarnetu { get; set; }

    public virtual SlownikSezon? Sezon { get; set; }
}

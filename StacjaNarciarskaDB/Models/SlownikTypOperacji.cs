using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class SlownikTypOperacji
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public virtual ICollection<Transakcja> Transakcjas { get; set; } = new List<Transakcja>();
}

using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class SlownikStatusKarnetu
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public virtual ICollection<Karnet> Karnets { get; set; } = new List<Karnet>();
}

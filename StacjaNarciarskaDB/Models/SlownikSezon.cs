using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class SlownikSezon
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public virtual ICollection<Taryfa> Taryfas { get; set; } = new List<Taryfa>();
}

using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class SlownikTrudnoscTrasy
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public virtual ICollection<Trasa> Trasas { get; set; } = new List<Trasa>();
}

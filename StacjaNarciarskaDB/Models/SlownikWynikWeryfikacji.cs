using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class SlownikWynikWeryfikacji
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public virtual ICollection<OdbicieBramka> OdbicieBramkas { get; set; } = new List<OdbicieBramka>();
}

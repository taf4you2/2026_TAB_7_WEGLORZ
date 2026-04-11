using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Kartum
{
    public string Id { get; set; } = null!;

    public int? StatusId { get; set; }

    public string? StanFizyczny { get; set; }

    public DateTime? DataDodaniaDoPuli { get; set; }

    public virtual ICollection<Karnet> Karnets { get; set; } = new List<Karnet>();

    public virtual ICollection<OdbicieBramka> OdbicieBramkas { get; set; } = new List<OdbicieBramka>();

    public virtual SlownikStatusKarty? Status { get; set; }
}

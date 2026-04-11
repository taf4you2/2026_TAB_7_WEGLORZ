using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Bramka
{
    public int Id { get; set; }

    public int? WyciagId { get; set; }

    public string? Nazwa { get; set; }

    public bool? CzyAktywna { get; set; }

    public virtual ICollection<OdbicieBramka> OdbicieBramkas { get; set; } = new List<OdbicieBramka>();

    public virtual Wyciag? Wyciag { get; set; }
}

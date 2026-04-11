using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Kasjer
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string HasloHash { get; set; } = null!;

    public bool? CzyAktywny { get; set; }

    public virtual ICollection<RaportZmianowy> RaportZmianowies { get; set; } = new List<RaportZmianowy>();

    public virtual ICollection<Transakcja> Transakcjas { get; set; } = new List<Transakcja>();
}

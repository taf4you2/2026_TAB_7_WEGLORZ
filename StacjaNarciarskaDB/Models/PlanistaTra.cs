using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class PlanistaTra
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string HasloHash { get; set; } = null!;

    public bool? CzyAktywny { get; set; }

    public virtual ICollection<Trasa> Trasas { get; set; } = new List<Trasa>();

    public virtual ICollection<Wyciag> Wyciags { get; set; } = new List<Wyciag>();
}

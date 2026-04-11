using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class RodzajRaportu
{
    public int Id { get; set; }

    public string Nazwa { get; set; } = null!;

    public string? Opis { get; set; }

    public virtual ICollection<RaportAdministratora> RaportAdministratoras { get; set; } = new List<RaportAdministratora>();
}

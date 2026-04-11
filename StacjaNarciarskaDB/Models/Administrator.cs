using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Administrator
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string HasloHash { get; set; } = null!;

    public bool? CzyAktywny { get; set; }

    public virtual ICollection<RaportAdministratora> RaportAdministratoras { get; set; } = new List<RaportAdministratora>();
}

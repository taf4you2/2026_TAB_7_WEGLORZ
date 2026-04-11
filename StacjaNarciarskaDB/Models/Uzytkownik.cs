using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Uzytkownik
{
    public int Id { get; set; }

    public string? Email { get; set; }

    public string? HasloHash { get; set; }

    public DateTime? DataUtworzenia { get; set; }

    public virtual ICollection<Rezerwacja> Rezerwacjas { get; set; } = new List<Rezerwacja>();
}

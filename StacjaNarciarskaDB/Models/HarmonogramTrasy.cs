using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class HarmonogramTrasy
{
    public int Id { get; set; }

    public int? TrasaId { get; set; }

    public bool? CzyOtwarta { get; set; }

    public string? PowodZamkniecia { get; set; }

    public DateTime? DataZmiany { get; set; }

    public virtual Trasa? Trasa { get; set; }
}

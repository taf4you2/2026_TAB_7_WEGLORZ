using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class OdbicieBramka
{
    public int Id { get; set; }

    public string? KartaId { get; set; }

    public int? BramkaId { get; set; }

    public DateTime? CzasOdbicia { get; set; }

    public DateTime? BlokadaCzasowaDo { get; set; }

    public int? WynikWeryfikacjiId { get; set; }

    public virtual Bramka? Bramka { get; set; }

    public virtual Kartum? Karta { get; set; }

    public virtual SlownikWynikWeryfikacji? WynikWeryfikacji { get; set; }
}

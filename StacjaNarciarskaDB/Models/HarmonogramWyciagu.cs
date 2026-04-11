using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class HarmonogramWyciagu
{
    public int Id { get; set; }

    public int? WyciagId { get; set; }

    public int? DzienTygodnia { get; set; }

    public TimeOnly? GodzinaOtwarcia { get; set; }

    public TimeOnly? GodzinaZamkniecia { get; set; }

    public virtual Wyciag? Wyciag { get; set; }
}

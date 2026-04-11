using System;
using System.Collections.Generic;

namespace StacjaNarciarskaDB.Models;

public partial class Karnet
{
    public int Id { get; set; }

    public string? KartaId { get; set; }

    public int? TaryfaId { get; set; }

    public int? RezerwacjaId { get; set; }

    public int? StatusId { get; set; }

    public DateTime? DataWaznosciOd { get; set; }

    public DateTime? DataWaznosciDo { get; set; }

    public string? PowodBlokady { get; set; }

    public virtual Kartum? Karta { get; set; }

    public virtual Rezerwacja? Rezerwacja { get; set; }

    public virtual SlownikStatusKarnetu? Status { get; set; }

    public virtual Taryfa? Taryfa { get; set; }
}

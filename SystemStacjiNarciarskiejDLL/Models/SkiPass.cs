using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("ski_pass")]
public class SkiPass
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("card_id")]
    public string? CardId { get; set; }
    public virtual Card? Card { get; set; }

    [Column("tariff_id")]
    public int? TariffId { get; set; }
    public virtual Tariff? Tariff { get; set; }

    [Column("reservation_id")]
    public int? ReservationId { get; set; }
    public virtual Reservation? Reservation { get; set; }

    [Column("status_id")]
    public int? StatusId { get; set; }
    public virtual DictPassStatus? Status { get; set; }

    [Column("valid_from")]
    public DateTime? ValidFrom { get; set; }

    [Column("valid_to")]
    public DateTime? ValidTo { get; set; }

    [Column("block_reason")]
    public string? BlockReason { get; set; }
}

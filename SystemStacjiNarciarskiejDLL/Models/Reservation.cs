using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("reservation")]
public class Reservation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("reservation_number")]
    public string ReservationNumber { get; set; } = null!;

    [Column("user_id")]
    public int? UserId { get; set; }
    public virtual User? User { get; set; }

    [Column("reservation_date")]
    public DateTime? ReservationDate { get; set; }

    [Column("status_id")]
    public int? StatusId { get; set; }
    public virtual DictReservationStatus? Status { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<SkiPass> SkiPasses { get; set; } = new List<SkiPass>();
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("lift_schedule")]
public class LiftSchedule
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("lift_id")]
    public int? LiftId { get; set; }
    public virtual Lift? Lift { get; set; }

    [Column("day_of_week")]
    public int? DayOfWeek { get; set; }

    [Column("opening_time")]
    public TimeSpan? OpeningTime { get; set; }

    [Column("closing_time")]
    public TimeSpan? ClosingTime { get; set; }
}

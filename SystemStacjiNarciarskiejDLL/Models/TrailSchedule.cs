using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("trail_schedule")]
public class TrailSchedule
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("trail_id")]
    public int? TrailId { get; set; }
    public virtual Trail? Trail { get; set; }

    [Column("is_open")]
    public bool? IsOpen { get; set; }

    [Column("closure_reason")]
    public string? ClosureReason { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

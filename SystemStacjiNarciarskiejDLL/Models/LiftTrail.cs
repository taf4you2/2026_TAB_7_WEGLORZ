using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("lift_trail")]
public class LiftTrail
{
    [Column("lift_id")]
    public int LiftId { get; set; }
    public virtual Lift Lift { get; set; } = null!;

    [Column("trail_id")]
    public int TrailId { get; set; }
    public virtual Trail Trail { get; set; } = null!;
}

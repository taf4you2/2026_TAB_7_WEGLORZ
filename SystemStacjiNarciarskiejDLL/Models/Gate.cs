using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("gate")]
public class Gate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("lift_id")]
    public int? LiftId { get; set; }
    public virtual Lift? Lift { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    public virtual ICollection<GateScan> GateScans { get; set; } = new List<GateScan>();
}

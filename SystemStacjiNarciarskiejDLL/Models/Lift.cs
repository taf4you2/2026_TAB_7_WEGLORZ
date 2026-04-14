using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("lift")]
public class Lift
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("location")]
    public string? Location { get; set; }

    [Column("length")]
    public decimal? Length { get; set; }

    [Column("planner_id")]
    public int? PlannerId { get; set; }

    public virtual TrailPlanner? Planner { get; set; }
    public virtual ICollection<LiftTrail> LiftTrails { get; set; } = new List<LiftTrail>();
    public virtual ICollection<LiftSchedule> Schedules { get; set; } = new List<LiftSchedule>();
    public virtual ICollection<Gate> Gates { get; set; } = new List<Gate>();
}

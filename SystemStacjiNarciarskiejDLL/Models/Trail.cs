using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("trail")]
public class Trail
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

    [Column("difficulty_id")]
    public int? DifficultyId { get; set; }
    public virtual DictTrailDifficulty? Difficulty { get; set; }

    [Column("planner_id")]
    public int? PlannerId { get; set; }
    public virtual TrailPlanner? Planner { get; set; }

    public virtual ICollection<LiftTrail> LiftTrails { get; set; } = new List<LiftTrail>();
    public virtual ICollection<TrailSchedule> Schedules { get; set; } = new List<TrailSchedule>();
}

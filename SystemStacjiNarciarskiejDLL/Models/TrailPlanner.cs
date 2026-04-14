using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("trail_planner")]
public class TrailPlanner
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("login")]
    public string Login { get; set; } = null!;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("is_active")]
    public bool? IsActive { get; set; }

    public virtual ICollection<Trail> Trails { get; set; } = new List<Trail>();
    public virtual ICollection<Lift> Lifts { get; set; } = new List<Lift>();
}

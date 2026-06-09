using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("tariff")]
public class Tariff
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("season_id")]
    public int? SeasonId { get; set; }
    public virtual DictSeason? Season { get; set; }

    [Column("pass_type_id")]
    public int? PassTypeId { get; set; }
    public virtual DictPassType? PassType { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [Column("ride_count")]
    public int? RideCount { get; set; }

    [Column("pool_limit")]
    public int? PoolLimit { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    public virtual ICollection<SkiPass> SkiPasses { get; set; } = new List<SkiPass>();
}

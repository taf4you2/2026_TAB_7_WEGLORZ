using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("card")]
public class Card
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = null!;

    [Column("status_id")]
    public int? StatusId { get; set; }
    public virtual DictCardStatus? Status { get; set; }

    [Column("physical_condition")]
    public string? PhysicalCondition { get; set; }

    [Column("added_to_pool_at")]
    public DateTime? AddedToPoolAt { get; set; }

    public virtual ICollection<SkiPass> SkiPasses { get; set; } = new List<SkiPass>();
    public virtual ICollection<GateScan> GateScans { get; set; } = new List<GateScan>();
}

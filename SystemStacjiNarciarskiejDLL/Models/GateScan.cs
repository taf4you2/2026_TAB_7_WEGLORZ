using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("gate_scan")]
public class GateScan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("card_id")]
    public string? CardId { get; set; }
    public virtual Card? Card { get; set; }

    [Column("gate_id")]
    public int? GateId { get; set; }
    public virtual Gate? Gate { get; set; }

    [Column("scan_time")]
    public DateTime? ScanTime { get; set; }

    [Column("time_blocked_until")]
    public DateTime? TimeBlockedUntil { get; set; }

    [Column("verification_result_id")]
    public int? VerificationResultId { get; set; }
    public virtual DictVerificationResult? VerificationResult { get; set; }
}

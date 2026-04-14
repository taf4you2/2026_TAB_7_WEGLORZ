using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("admin_report")]
public class AdminReport
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("admin_id")]
    public int? AdminId { get; set; }
    public virtual Administrator? Admin { get; set; }

    [Column("report_type_id")]
    public int? ReportTypeId { get; set; }
    public virtual DictReportType? ReportType { get; set; }

    [Column("generated_at")]
    public DateTime? GeneratedAt { get; set; }

    [Column("report_parameters")]
    public string? ReportParameters { get; set; }
}

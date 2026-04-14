using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("shift_report")]
public class ShiftReport
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("cashier_id")]
    public int? CashierId { get; set; }
    public virtual Cashier? Cashier { get; set; }

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("total_revenue")]
    public decimal? TotalRevenue { get; set; }

    [Column("total_deposit_returns")]
    public decimal? TotalDepositReturns { get; set; }

    [Column("cards_issued_count")]
    public int? CardsIssuedCount { get; set; }
}

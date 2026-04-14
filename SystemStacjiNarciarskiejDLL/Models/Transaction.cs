using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("transaction")]
public class Transaction
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("reservation_id")]
    public int? ReservationId { get; set; }
    public virtual Reservation? Reservation { get; set; }

    [Column("cashier_id")]
    public int? CashierId { get; set; }
    public virtual Cashier? Cashier { get; set; }

    [Column("operation_type_id")]
    public int? OperationTypeId { get; set; }
    public virtual DictOperationType? OperationType { get; set; }

    [Required]
    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("transaction_date")]
    public DateTime? TransactionDate { get; set; }
}

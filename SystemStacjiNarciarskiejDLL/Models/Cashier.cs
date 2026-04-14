using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SystemStacjiNarciarskiejDLL.Models;

[Table("cashier")]
public class Cashier
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

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<ShiftReport> ShiftReports { get; set; } = new List<ShiftReport>();
}

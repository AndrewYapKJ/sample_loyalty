using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Payment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string StoreId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PaymentGateway { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "datetime2")]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? AppointmentId { get; set; }

        [StringLength(450)]
        public string? PackagePurchaseId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Store Store { get; set; } = null!;
        public virtual Appointment? Appointment { get; set; }
        public virtual UserPackage? PackagePurchase { get; set; }

        public virtual ICollection<Appointment> AppointmentsWithPayment { get; set; } = new List<Appointment>();
        public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();
    }
}
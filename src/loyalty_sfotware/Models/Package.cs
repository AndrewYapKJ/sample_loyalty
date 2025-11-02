using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Package
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int TotalSessions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int ValidDays { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? PackageTypeId { get; set; }

        // Navigation properties
        public virtual PackageType? PackageType { get; set; }
        public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();
    }

    public class PackageType
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
    }

    public class UserPackage
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string PackageId { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public int RemainingSessions { get; set; }

        [Column(TypeName = "date")]
        public DateTime ExpiryDate { get; set; }

        [StringLength(450)]
        public string? PaymentId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Package Package { get; set; } = null!;
        public virtual Payment? Payment { get; set; }

        public virtual ICollection<PackageUsage> PackageUsages { get; set; } = new List<PackageUsage>();
        public virtual ICollection<Payment> PaymentsForPackage { get; set; } = new List<Payment>();
    }

    public class PackageUsage
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserPackageId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? AppointmentId { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime UsedOn { get; set; } = DateTime.UtcNow;

        public int SessionsUsed { get; set; } = 1;

        // Navigation properties
        public virtual UserPackage UserPackage { get; set; } = null!;

        public virtual ICollection<Appointment> AppointmentsWithUsage { get; set; } = new List<Appointment>();
        public virtual ICollection<UserFeedback> Feedback { get; set; } = new List<UserFeedback>();
    }
}
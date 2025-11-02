using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Appointment
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string StoreId { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime AppointmentTime { get; set; }

        [StringLength(450)]
        public string? ServiceId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Scheduled";

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? PaymentId { get; set; }

        [StringLength(450)]
        public string? PackageUsageId { get; set; }

        [StringLength(450)]
        public string? IntegrationId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Store Store { get; set; } = null!;
        public virtual Service? Service { get; set; }
        public virtual Payment? Payment { get; set; }
        public virtual PackageUsage? PackageUsage { get; set; }
        public virtual Integration? Integration { get; set; }

        public virtual ICollection<LoyaltyPoint> LoyaltyPoints { get; set; } = new List<LoyaltyPoint>();
        public virtual ICollection<UserFeedback> Feedback { get; set; } = new List<UserFeedback>();
    }
}
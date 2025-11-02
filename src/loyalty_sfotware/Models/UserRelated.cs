using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class StoreGroup
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    }

    public class UserPreferences
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        public bool MarketingOptIn { get; set; } = true;

        [StringLength(50)]
        public string CommunicationChannel { get; set; } = "Email";

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    public class UserAddress
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine1 { get; set; }

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    public class UserDevice
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(50)]
        public string DeviceType { get; set; } = string.Empty;

        [StringLength(200)]
        public string DeviceId { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    public class UserConsent
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string ConsentType { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime ConsentGivenAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    public class StoreHour
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string StoreId { get; set; } = string.Empty;

        [StringLength(20)]
        public string DayOfWeek { get; set; } = string.Empty;

        [Column(TypeName = "time")]
        public TimeOnly OpenTime { get; set; }

        [Column(TypeName = "time")]
        public TimeOnly CloseTime { get; set; }

        // Navigation properties
        public virtual Store Store { get; set; } = null!;
    }

    public class UserFeedback
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? AppointmentId { get; set; }

        [StringLength(450)]
        public string? RedemptionId { get; set; }

        [StringLength(450)]
        public string? RewardId { get; set; }

        [StringLength(450)]
        public string? EventId { get; set; }

        [StringLength(450)]
        public string? PackageUsageId { get; set; }

        [Column(TypeName = "ntext")]
        public string? FeedbackText { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual Appointment? Appointment { get; set; }

        public virtual Redemption? Redemption { get; set; }

        public virtual Reward? Reward { get; set; }

        public virtual Event? Event { get; set; }

        public virtual PackageUsage? PackageUsage { get; set; }
    }

    public class NotificationLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string NotificationType { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column(TypeName = "datetime2")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
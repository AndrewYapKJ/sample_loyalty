using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Category
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
        public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();
        public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }

    public class UserActivity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string ActivityType { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    public class StoreSettings
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string StoreId { get; set; } = string.Empty;

        public bool LoyaltyEnabled { get; set; } = true;

        [Column(TypeName = "nvarchar(max)")]
        public string? Config { get; set; }

        // Navigation properties
        public virtual Store Store { get; set; } = null!;
    }

    public class TierHistory
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string TierId { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual MembershipTier Tier { get; set; } = null!;
    }

    public class Integration
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string SystemType { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? ConfigDetails { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    public class UserActivityLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string ActivityType { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
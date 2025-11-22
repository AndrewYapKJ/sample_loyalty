using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? ReferralCode { get; set; }

        [StringLength(50)]
        public string? ReferredBy { get; set; }

        [StringLength(450)]
        public string? CurrentTierId { get; set; }

        public int TotalPoints { get; set; } = 0;

        public int TotalReferrals { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual MembershipTier? CurrentTier { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<LoyaltyPoint> LoyaltyPoints { get; set; } = new List<LoyaltyPoint>();
        public virtual ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
        public virtual ICollection<Referral> ReferralsGiven { get; set; } = new List<Referral>();
        public virtual ICollection<Referral> ReferralsReceived { get; set; } = new List<Referral>();
        public virtual ICollection<UserPackage> UserPackages { get; set; } = new List<UserPackage>();
        public virtual ICollection<UserActivityLog> ActivityLogs { get; set; } = new List<UserActivityLog>();
        public virtual ICollection<TierHistory> TierHistory { get; set; } = new List<TierHistory>();
        public virtual ICollection<PointExpiry> PointExpiries { get; set; } = new List<PointExpiry>();
        public virtual ICollection<UserEventParticipation> EventParticipations { get; set; } = new List<UserEventParticipation>();
        public virtual UserPreferences? Preferences { get; set; }
        public virtual ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public virtual ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
        public virtual ICollection<UserConsent> Consents { get; set; } = new List<UserConsent>();
        public virtual ICollection<UserFeedback> Feedback { get; set; } = new List<UserFeedback>();
        public virtual ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
    }
}
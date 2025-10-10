using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Redemption
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string RewardId { get; set; } = string.Empty;

        public int PointsRedeemed { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime RedemptionDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? StoreId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(450)]
        public string? MethodId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual Reward Reward { get; set; } = null!;

        public virtual Store? Store { get; set; }

        public virtual RewardRedemptionMethod? Method { get; set; }

        public virtual ICollection<LoyaltyPoint> LoyaltyPoints { get; set; } = new List<LoyaltyPoint>();
        public virtual ICollection<UserFeedback> Feedback { get; set; } = new List<UserFeedback>();
    }
}
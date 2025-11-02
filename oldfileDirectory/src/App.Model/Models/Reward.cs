using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Reward
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int PointsRequired { get; set; }

        public bool IsSeasonal { get; set; } = false;

        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? CategoryId { get; set; }

        // Navigation properties
        public virtual RewardCategory? Category { get; set; }
        public virtual ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
        public virtual ICollection<EventReward> EventRewards { get; set; } = new List<EventReward>();
        public virtual ICollection<RewardInventory> Inventory { get; set; } = new List<RewardInventory>();
        public virtual ICollection<UserFeedback> Feedback { get; set; } = new List<UserFeedback>();
    }

    public class RewardCategory
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Reward> Rewards { get; set; } = new List<Reward>();
    }

    public class RewardInventory
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string RewardId { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Reward Reward { get; set; } = null!;
    }

    public class RewardRedemptionMethod
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
    }
}
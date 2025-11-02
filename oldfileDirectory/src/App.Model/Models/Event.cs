using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Event
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<EventReward> EventRewards { get; set; } = new List<EventReward>();
        public virtual ICollection<UserEventParticipation> UserParticipations { get; set; } = new List<UserEventParticipation>();
        public virtual ICollection<UserFeedback> Feedback { get; set; } = new List<UserFeedback>();
    }

    public class EventReward
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string EventId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string RewardId { get; set; } = string.Empty;

        // Navigation properties
        public virtual Event Event { get; set; } = null!;

        public virtual Reward Reward { get; set; } = null!;
    }

    public class UserEventParticipation
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string EventId { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime ParticipationDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual Event Event { get; set; } = null!;
    }
}
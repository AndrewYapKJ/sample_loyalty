using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Referral
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string ReferrerUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string ReferredUserId { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime ReferralDate { get; set; } = DateTime.UtcNow;

        public int BonusPoints { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(450)]
        public string? ReferralCampaignId { get; set; }

        // Navigation properties
        public virtual User ReferrerUser { get; set; } = null!;

        public virtual User ReferredUser { get; set; } = null!;

        public virtual ReferralCampaign? ReferralCampaign { get; set; }
    }

    public class ReferralCampaign
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public int BonusPoints { get; set; }

        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        // Navigation properties
        public virtual ICollection<Referral> Referrals { get; set; } = new List<Referral>();
    }
}
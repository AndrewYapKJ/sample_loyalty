using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class LoyaltyPoint
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string Source { get; set; } = string.Empty;

        public int Points { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? RelatedAppointmentId { get; set; }

        [StringLength(450)]
        public string? RelatedRedemptionId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;

        public virtual Appointment? RelatedAppointment { get; set; }

        public virtual Redemption? RelatedRedemption { get; set; }

        public virtual ICollection<PointExpiry> PointExpiries { get; set; } = new List<PointExpiry>();
    }

    public class PointExpiry
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string LoyaltyPointId { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime ExpiryDate { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual LoyaltyPoint LoyaltyPoint { get; set; } = null!;
    }
}
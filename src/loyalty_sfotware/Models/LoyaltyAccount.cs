using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class LoyaltyAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PointsBalance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimePointsEarned { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LifetimePointsRedeemed { get; set; } = 0;

        [StringLength(50)]
        public string TierLevel { get; set; } = "Bronze";

        [Column(TypeName = "datetime2")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime? LastActivityDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
    }
}
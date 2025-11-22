using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public enum PointTransactionType
    {
        Earned,
        Redeemed,
        Expired,
        Adjusted
    }

    public class PointTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LoyaltyAccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Points { get; set; }

        [Required]
        public PointTransactionType TransactionType { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? ReferenceNumber { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime? ExpirationDate { get; set; }

        public bool IsProcessed { get; set; } = true;

        // Navigation properties
        public virtual LoyaltyAccount LoyaltyAccount { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PointsEarned { get; set; }

        [StringLength(100)]
        public string? Store { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public bool IsProcessed { get; set; } = false;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
    }
}
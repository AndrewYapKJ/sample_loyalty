using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime? LastModified { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<LoyaltyAccount> LoyaltyAccounts { get; set; } = new List<LoyaltyAccount>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
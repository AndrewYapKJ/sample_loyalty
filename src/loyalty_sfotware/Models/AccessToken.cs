using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class AccessToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string UserType { get; set; } = string.Empty; // "admin" or "customer"

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime ExpiresAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime RefreshTokenExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string? DeviceId { get; set; }

        [StringLength(100)]
        public string? DeviceType { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? LastUsedAt { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? RevokedAt { get; set; }

        [StringLength(200)]
        public string? RevokedReason { get; set; }

        // Navigation properties (no foreign keys)
        public virtual User? User { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Admin
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "Admin";

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime? LastLoginAt { get; set; }

        public int LoginAttempts { get; set; } = 0;

        [Column(TypeName = "datetime2")]
        public DateTime? LockedUntil { get; set; }

        // Navigation properties
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public virtual ICollection<AdminAuditLog> AuditLogs { get; set; } = new List<AdminAuditLog>();
    }

    public class RefreshToken
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string AdminId { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string JwtId { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        public bool IsRevoked { get; set; } = false;

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2")]
        public DateTime ExpiresAt { get; set; }

        [StringLength(50)]
        public string? DeviceInfo { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        // Navigation properties
        public virtual Admin Admin { get; set; } = null!;
    }

    public class AdminAuditLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(450)]
        public string AdminId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Admin Admin { get; set; } = null!;
    }
}
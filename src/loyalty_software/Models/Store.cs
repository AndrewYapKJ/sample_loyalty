using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gussmann_loyalty_program.Models
{
    public class Store
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? StoreGroupId { get; set; }

        // Navigation properties
        public virtual StoreGroup? StoreGroup { get; set; }
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Redemption> Redemptions { get; set; } = new List<Redemption>();
        public virtual StoreSettings? Settings { get; set; }
        public virtual ICollection<StoreHour> Hours { get; set; } = new List<StoreHour>();
    }
}
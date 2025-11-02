using System.ComponentModel.DataAnnotations;

namespace gussmann_loyalty_program.Models
{
    public class MembershipTier
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int MinPoints { get; set; }

        [StringLength(1000)]
        public string? Benefits { get; set; }

        public int TierOrder { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<TierHistory> TierHistory { get; set; } = new List<TierHistory>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class CustomerReview
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Client")]
        public string ClientId { get; set; } = null!;

        public AppUser? Client { get; set; }

        [Required]
        public string Comment { get; set; } = null!;

        [Required]
        public int Rating { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public string? Reply { get; set; }

        public DateTime? RepliedAt { get; set; }
    }
}
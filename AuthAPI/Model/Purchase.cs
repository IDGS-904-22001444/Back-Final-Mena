using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class Purchase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Provider")]
        public int ProviderId { get; set; }

        [Required]
        [ForeignKey("Admin")]
        public string AdminId { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required]
        public int Status { get; set; }

        // Relaciones de navegación (opcional)
        public Provider? Provider { get; set; }
        public AppUser? Admin { get; set; }
    }
}
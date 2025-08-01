using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("AppUser")]
        public string ClientId { get; set; } = null!; // GUID como string

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime SaleDate { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required]
        public int Status { get; set; }

        // Relaciones de navegación
        public AppUser? Client { get; set; }
        public Product? Product { get; set; }
    }
}
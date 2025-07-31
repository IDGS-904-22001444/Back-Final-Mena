using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class ProductMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        [ForeignKey("RawMaterial")]
        public int RawMaterialId { get; set; }

        [Required]
        public decimal RequiredQuantity { get; set; }

        [Required]
        public int Status { get; set; }

        // Relaciones de navegación (opcional)
        public Product? Product { get; set; }
        public RawMaterial? RawMaterial { get; set; }
    }
}
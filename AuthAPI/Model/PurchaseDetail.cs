using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class PurchaseDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Purchase")]
        public int PurchaseId { get; set; }

        [Required]
        [ForeignKey("RawMaterial")]
        public int RawMaterialId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal Subtotal { get; set; }

        [Required]
        public int Status { get; set; }

        // Relaciones de navegación (opcional)
        public Purchase? Purchase { get; set; }
        public RawMaterial? RawMaterial { get; set; }
    }
}
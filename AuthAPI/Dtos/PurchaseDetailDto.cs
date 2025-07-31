using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class PurchaseDetailDto
    {
        // No se requiere Id_Detalle si es autoincremental en la BD
        [Required]
        public int PurchaseId { get; set; }
        [Required]
        public int RawMaterialId { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        [Required]
        public decimal Subtotal { get; set; }
        [Required]
        public int Status { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class SaleDto
    {
        // No se requiere Id_Venta si es autoincremental en la BD
        [Required]
        public int ClientId { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public DateTime SaleDate { get; set; }
        [Required]
        public decimal Total { get; set; }
        [Required]
        public int Status { get; set; }
    }

}

using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class ProductDto
    {
        // No se requiere Id_Producto si es autoincremental en la BD
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        public decimal SalePrice { get; set; }
        [Required]
        public int Stock { get; set; }
        [Required]
        public int Status { get; set; }
    }
}

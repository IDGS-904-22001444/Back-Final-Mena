using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class ProductMaterialDto
    {
        // No se requiere Id_Material_Producto si es autoincremental en la BD
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int RawMaterialId { get; set; }
        [Required]
        public decimal RequiredQuantity { get; set; }
        [Required]
        public int Status { get; set; }
    }
}

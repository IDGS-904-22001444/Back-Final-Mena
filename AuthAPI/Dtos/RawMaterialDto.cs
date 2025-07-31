using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class RawMaterialDto
    {
        // No se requiere Id_Materia si es autoincremental en la BD
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        public string UnitOfMeasure { get; set; } = null!;
        [Required]
        public decimal UnitCost { get; set; }
        [Required]
        public int Stock { get; set; }
        [Required]
        public int Status { get; set; }
    }
}

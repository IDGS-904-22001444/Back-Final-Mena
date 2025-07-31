using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Model
{
    public class RawMaterial
    {
        [Key]
        public int Id { get; set; }

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
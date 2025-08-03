using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class ProductionOrderDto
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int QuantityToProduce { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class SaleDto
    {
        [Required]
        public string ClientId { get; set; } = null!; // GUID como string

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

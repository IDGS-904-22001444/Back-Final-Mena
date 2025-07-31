using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class PurchaseDto
    {
        [Required]
        public int ProviderId { get; set; }

        [Required]
        public string AdminId { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [Required]
        public decimal Total { get; set; }

        [Required]
        public int Status { get; set; }

        // Agrega la lista de detalles de compra
        public List<PurchaseDetailDto>? Details { get; set; }
    }
}
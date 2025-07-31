using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class Quotation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [ForeignKey("QuotationStatus")]
        public int QuotationStatusId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public decimal Total { get; set; }

        public string? Requirements { get; set; }

        // Relaciones de navegación (opcional)
        // public Client? Client { get; set; }
        // public Product? Product { get; set; }
        // public QuotationStatus? QuotationStatus { get; set; }
    }
}
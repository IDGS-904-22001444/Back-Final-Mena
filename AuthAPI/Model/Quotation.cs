using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class Quotations
    {
        [Key]
        public int Id { get; set; }

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

        // Relación con el usuario que solicita la cotización
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; } = null!; // El Id del usuario autenticado

        public AppUser? User { get; set; }
    }
}
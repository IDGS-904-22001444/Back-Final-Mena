using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class QuotationRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string Phone { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Country { get; set; } = null!;

        [Required]
        public string Region { get; set; } = null!;

        public string? Company { get; set; }

        public string? AnimalType { get; set; }

        // Necesidades (checkboxes)
        public bool NeedHabitatSystem { get; set; }
        public bool NeedBiologyResearch { get; set; }
        public bool NeedZoosAquariums { get; set; }
        public bool NeedNaturalReserves { get; set; }
        public bool NeedOther { get; set; }

        public string? Comments { get; set; }

        public bool AcceptsInfo { get; set; }

        // Nuevo campo para el producto cotizado
        public int? ProductId { get; set; }

        // Relación de navegación opcional
        // public Product? Product { get; set; }
    }
}
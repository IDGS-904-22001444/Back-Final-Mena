using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class QuotationRequestDto
    {
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

        public bool NeedHabitatSystem { get; set; }
        public bool NeedBiologyResearch { get; set; }
        public bool NeedZoosAquariums { get; set; }
        public bool NeedNaturalReserves { get; set; }
        public bool NeedOther { get; set; }

        public string? Comments { get; set; }

        public bool AcceptsInfo { get; set; }
    }
}
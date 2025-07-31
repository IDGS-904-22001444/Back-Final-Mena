using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class ProviderDto
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Phone { get; set; } = null!;
        [Required]
        [EmailAddress] // Agregado para validación de formato de email
        public string Email { get; set; } = null!;
        [Required]
        public string Address { get; set; } = null!;
        [Required]
        public string ContactPerson { get; set; } = null!;
        [Required]
        public int Status { get; set; }
    }
}

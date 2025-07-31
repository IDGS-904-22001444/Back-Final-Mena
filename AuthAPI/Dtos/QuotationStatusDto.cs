using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class QuotationStatusDto
    {
        // No se requiere Id_Estado si es autoincremental en la BD
        [Required]
        public string StatusName { get; set; } = null!;
    }
}

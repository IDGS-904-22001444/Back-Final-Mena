using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class GeneralStatusDto
    {
        // No se requiere Id_Estatus si es autoincremental en la BD
        [Required]
        public string StatusName { get; set; } = null!;
    }
}

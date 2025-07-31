using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Dtos
{
    public class RawMaterialMovementDto
    {
        // No se requiere Id_Movimiento si es autoincremental en la BD
        [Required]
        public int RawMaterialId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        // [Required] para Entrada y Salida depende de tu lógica de negocio
        // Podrías tener un tipo de movimiento y solo uno de los dos es requerido.
        // Aquí los dejo como opcionales si solo uno se usa por movimiento.
        public int? EntryQuantity { get; set; } // Puede ser nulo si es una salida
        public int? ExitQuantity { get; set; }  // Puede ser nulo si es una entrada
        [Required]
        public int CurrentStock { get; set; }
        [Required]
        public decimal Cost { get; set; }
        [Required]
        public decimal Average { get; set; }
        [Required]
        public decimal Debit { get; set; }
        [Required]
        public decimal Credit { get; set; } // Renombrado de 'Hecho' a 'Credit' para mayor claridad contable
        [Required]
        public decimal Balance { get; set; }
        [Required]
        public int Status { get; set; }
    }
}

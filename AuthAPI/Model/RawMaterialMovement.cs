using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Model
{
    public class RawMaterialMovement
    {
        [Key]
        public int MovementId { get; set; } // Id_Movimiento
        public int RawMaterialId { get; set; }
        public DateTime Date { get; set; }
        public int? EntryQuantity { get; set; }
        public int? ExitQuantity { get; set; }
        public int CurrentStock { get; set; }
        public decimal Cost { get; set; }
        public decimal Average { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public int Status { get; set; }
    }
}

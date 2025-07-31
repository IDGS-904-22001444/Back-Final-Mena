using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Model
{
    public class QuotationStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StatusName { get; set; } = null!;
    }
}
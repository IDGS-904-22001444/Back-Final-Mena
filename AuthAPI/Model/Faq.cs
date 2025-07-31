using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Model
{
    public class Faq
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }
    }
}
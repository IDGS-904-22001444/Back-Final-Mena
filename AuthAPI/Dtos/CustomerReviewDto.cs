namespace AuthAPI.Dtos
{
    public class CustomerReviewDto
    {
        public string ClientId { get; set; } // Cambiado a string
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; } // Nuevo campo
    }
}
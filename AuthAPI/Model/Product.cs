namespace AuthAPI.Model
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal SalePrice { get; set; }
        public int Stock { get; set; }
        public int Status { get; set; }
        public string? ImageUrl { get; set; } // Nueva propiedad para la imagen
    }
}

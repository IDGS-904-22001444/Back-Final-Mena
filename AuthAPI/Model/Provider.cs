namespace AuthAPI.Model
{
    public class Provider
    {
        public int ProviderId { get; set; } // Id_Proveedor
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string ContactPerson { get; set; } = null!;
        public int Status { get; set; }
    }
}

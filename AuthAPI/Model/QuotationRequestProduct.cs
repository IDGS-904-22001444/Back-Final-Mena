using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthAPI.Model
{
    public class QuotationRequestProduct
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("QuotationRequest")]
        public int QuotationRequestId { get; set; }
        public QuotationRequest QuotationRequest { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
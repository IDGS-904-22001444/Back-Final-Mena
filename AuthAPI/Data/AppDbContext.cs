using AuthAPI.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Data
{
    public class AppDbContext : IdentityDbContext <AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Provider> Providers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<RawMaterialMovement> RawMaterialMovements { get; set; }
        public DbSet<PurchaseDetail> PurchaseDetails { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<ProductMaterial> ProductMaterials { get; set; }
        public DbSet<GeneralStatus> GeneralStatuses { get; set; }
        public DbSet<QuotationStatus> QuotationStatuses { get; set; }
        public DbSet<Quotations> Quotations { get; set; }
        public DbSet<CustomerReview> CustomerReviews { get; set; }

        public DbSet<Faq> Faqs { get; set; }
    }
}


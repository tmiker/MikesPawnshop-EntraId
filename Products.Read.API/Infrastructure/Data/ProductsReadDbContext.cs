using Microsoft.EntityFrameworkCore;
using Products.Read.API.Domain.Models;
using Products.Read.API.Infrastructure.EntityConfigurations;

namespace Products.Read.API.Infrastructure.Data
{
    public class ProductsReadDbContext : DbContext
    {
        public ProductsReadDbContext(DbContextOptions<ProductsReadDbContext> options) : base(options)
        { }
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ImageData> ImageData { get; set; } = null!;
        public DbSet<DocumentData> DocumentData { get; set; } = null!;
        public DbSet<ProductMessageRecord> ProductMessageRecords { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ImageDataConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentDataConfiguration());
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
        }
    }
}

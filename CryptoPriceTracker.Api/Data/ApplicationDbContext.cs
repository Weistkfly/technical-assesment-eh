using Microsoft.EntityFrameworkCore;
using CryptoPriceTracker.Api.Models;

namespace CryptoPriceTracker.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<CryptoAsset> CryptoAssets { get; set; }
        public DbSet<CryptoPriceHistory> CryptoPriceHistories { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CryptoAsset>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Symbol).IsRequired();
                entity.Property(e => e.ExternalId).IsRequired();
                entity.Property(e => e.Currency).IsRequired();
                entity.Property(e => e.IconUrl).IsRequired();
               
            });

            modelBuilder.Entity<CryptoPriceHistory>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Price).IsRequired();
                entity.Property(e => e.Date).IsRequired();

                entity.HasOne(d => d.CryptoAsset) 
                      .WithMany(p => p.PriceHistory) 
                      .HasForeignKey(d => d.CryptoAssetId) 
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
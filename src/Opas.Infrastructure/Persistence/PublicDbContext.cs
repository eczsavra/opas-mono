using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence.Ref;

namespace Opas.Infrastructure.Persistence;

public sealed class PublicDbContext : DbContext
{
    public PublicDbContext(DbContextOptions<PublicDbContext> options) : base(options) { }

    public DbSet<ProductRef> Products => Set<ProductRef>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ProductRef>(e =>
        {
            e.ToTable("product_ref");
            e.HasKey(x => x.Id);
            e.Property(x => x.Gtin).HasMaxLength(20).IsRequired();      // GTIN/Barcode
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Form).HasMaxLength(50);
            e.Property(x => x.Strength).HasMaxLength(50);
            e.Property(x => x.Manufacturer).HasMaxLength(120);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.HasIndex(x => x.Gtin).IsUnique();
            e.HasIndex(x => x.Name);
        });
    }
}

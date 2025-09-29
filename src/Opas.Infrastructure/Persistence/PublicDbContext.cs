using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence.Ref;
using Opas.Shared.ControlPlane;
using Opas.Domain.Entities;
using Opas.Domain.ValueObjects;
using System.Text.Json;

namespace Opas.Infrastructure.Persistence;

public sealed class PublicDbContext : DbContext
{
    public PublicDbContext(DbContextOptions<PublicDbContext> options) : base(options) { }

    // YENİ: Public DB'ye taşınan tablolar
    public DbSet<GlnRecord> GlnRegistry => Set<GlnRecord>();
    public DbSet<CentralProduct> CentralProducts => Set<CentralProduct>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // GlnRecord configuration (Public DB'ye taşındı)
        b.Entity<GlnRecord>(e =>
        {
            e.ToTable("gln_registry");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Gln).HasColumnName("gln").HasMaxLength(13).IsRequired();
            e.HasIndex(x => x.Gln).IsUnique();

            // ITS stakeholder fields
            e.Property(x => x.CompanyName).HasColumnName("company_name").HasMaxLength(200);
            e.Property(x => x.Authorized).HasColumnName("authorized").HasMaxLength(200);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(200);
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(50);
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
            e.Property(x => x.Town).HasColumnName("town").HasMaxLength(100);
            e.Property(x => x.Address).HasColumnName("address").HasMaxLength(500);
            e.Property(x => x.Active).HasColumnName("active");
            e.Property(x => x.Source).HasColumnName("source").HasMaxLength(50);

            e.Property(x => x.ImportedAtUtc).HasColumnName("imported_at_utc");
        });

        // CentralProduct configuration (Public DB'ye taşındı)
        b.Entity<CentralProduct>(e =>
        {
            e.ToTable("central_products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            
            // GTIN is unique globally
            e.HasIndex(x => x.Gtin).IsUnique();
            e.Property(x => x.Gtin).HasColumnName("gtin").HasMaxLength(50).IsRequired();
            
            // Drug info
            e.Property(x => x.DrugName).HasColumnName("drug_name").HasMaxLength(500).IsRequired();
            e.Property(x => x.ManufacturerGln).HasColumnName("manufacturer_gln").HasMaxLength(13);
            e.Property(x => x.ManufacturerName).HasColumnName("manufacturer_name").HasMaxLength(500);
            
            // ITS fields
            e.Property(x => x.Active).HasColumnName("active");
            e.Property(x => x.Imported).HasColumnName("imported");
            
            // BaseEntity fields
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            
            // Indexes for performance
            e.HasIndex(x => x.DrugName);
            e.HasIndex(x => x.Active);
            e.HasIndex(x => x.ManufacturerGln);
            
            // Soft delete filter
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}

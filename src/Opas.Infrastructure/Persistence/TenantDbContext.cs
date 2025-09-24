using Microsoft.EntityFrameworkCore;
using Opas.Shared.MultiTenancy;
using Opas.Shared.Tenant;

namespace Opas.Infrastructure.Persistence;

/// <summary>
/// Tenant-specific DbContext for pharmacy operations
/// Each tenant (pharmacy) has isolated data
/// </summary>
public sealed class TenantDbContext : DbContext
{
    public string TenantId { get; }

    // Pharmacy Business Entities - Tenant Isolated
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    
    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantProvider tenantProvider) : base(options)
    {
        TenantId = tenantProvider?.TenantId ?? "default";
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Customer Entity Configuration
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TcNumber).IsUnique();
            e.HasIndex(x => x.Phone);
            e.HasIndex(x => x.Email);
        });

        // Stock Entity Configuration
        modelBuilder.Entity<Stock>(e =>
        {
            e.ToTable("stocks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Gtin).IsUnique();
            e.HasIndex(x => x.ProductName);
            e.HasIndex(x => x.ExpiryDate);
            e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
            e.Property(x => x.SalePrice).HasPrecision(18, 2);
        });

        // Sale Entity Configuration
        modelBuilder.Entity<Sale>(e =>
        {
            e.ToTable("sales");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SaleNumber).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.SaleDate);
            e.HasIndex(x => x.Status);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.NetAmount).HasPrecision(18, 2);

            // Relationships
            e.HasOne(x => x.Customer)
             .WithMany()
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Prescription)
             .WithMany(p => p.Sales)
             .HasForeignKey(x => x.PrescriptionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // SaleItem Entity Configuration
        modelBuilder.Entity<SaleItem>(e =>
        {
            e.ToTable("sale_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SaleId);
            e.HasIndex(x => x.StockId);
            e.HasIndex(x => x.Gtin);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);

            // Relationships
            e.HasOne(x => x.Sale)
             .WithMany(s => s.SaleItems)
             .HasForeignKey(x => x.SaleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Stock)
             .WithMany()
             .HasForeignKey(x => x.StockId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Prescription Entity Configuration
        modelBuilder.Entity<Prescription>(e =>
        {
            e.ToTable("prescriptions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PrescriptionNumber).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.PrescriptionDate);
            e.HasIndex(x => x.Status);
            e.Property(x => x.InsuranceCoverage).HasPrecision(5, 2);

            // Relationships
            e.HasOne(x => x.Customer)
             .WithMany()
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PrescriptionItem Entity Configuration
        modelBuilder.Entity<PrescriptionItem>(e =>
        {
            e.ToTable("prescription_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PrescriptionId);
            e.HasIndex(x => x.Gtin);
            e.HasIndex(x => x.Status);

            // Relationships
            e.HasOne(x => x.Prescription)
             .WithMany(p => p.PrescriptionItems)
             .HasForeignKey(x => x.PrescriptionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}

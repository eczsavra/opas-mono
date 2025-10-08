using Microsoft.EntityFrameworkCore;
using Opas.Domain.Entities;
using Opas.Domain.ValueObjects;
using Opas.Shared.MultiTenancy;
using Opas.Shared.Tenant;
using System.Text.Json;

namespace Opas.Infrastructure.Persistence;

/// <summary>
/// Tenant-specific DbContext for pharmacy operations
/// Each tenant (pharmacy) has isolated data
/// </summary>
public sealed class TenantDbContext : DbContext
{
    public string TenantId { get; }

    // Pharmacy Business Entities - Tenant Isolated
    public DbSet<TenantProduct> Products => Set<TenantProduct>();
    
    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantProvider tenantProvider) : base(options)
    {
        TenantId = tenantProvider?.TenantId ?? "default";
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Default schema for tenant database
        modelBuilder.HasDefaultSchema("public");

        // TenantProduct Entity Configuration (aligned with CentralProduct)
        modelBuilder.Entity<TenantProduct>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            
            // GTIN is unique globally
            e.HasIndex(x => x.Gtin).IsUnique();
            e.Property(x => x.Gtin).HasColumnName("gtin").HasMaxLength(50).IsRequired();
            
            // Drug info
            e.Property(x => x.DrugName).HasColumnName("drug_name").HasMaxLength(500).IsRequired();
            e.Property(x => x.ManufacturerGln).HasColumnName("manufacturer_gln").HasMaxLength(50);
            e.Property(x => x.ManufacturerName).HasColumnName("manufacturer_name").HasMaxLength(500);
            
            // Price fields
            e.Property(x => x.Price).HasColumnName("price").HasColumnType("numeric(18,4)");
            e.Property(x => x.PriceHistory).HasColumnName("price_history").HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<PriceHistoryEntry>>(v, (JsonSerializerOptions?)null) ?? new List<PriceHistoryEntry>()
                );
            
            // ITS fields
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.LastItsSyncAt).HasColumnName("last_its_sync_at");
            
            // Product category and tracking fields
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(50);
            e.Property(x => x.HasDatamatrix).HasColumnName("has_datamatrix");
            e.Property(x => x.RequiresExpiryTracking).HasColumnName("requires_expiry_tracking");
            e.Property(x => x.IsControlled).HasColumnName("is_controlled");
            
            // User tracking fields
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
            
            // BaseEntity fields
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            
            // Indexes for performance
            e.HasIndex(x => x.DrugName);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.LastItsSyncAt);
            e.HasIndex(x => x.ManufacturerGln);
            
            // Soft delete filter
            e.HasQueryFilter(x => !x.IsDeleted);
        });


        base.OnModelCreating(modelBuilder);
    }
}

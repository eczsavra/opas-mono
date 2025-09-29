using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Opas.Infrastructure.Persistence;
using Opas.Domain.Entities;
using Opas.Domain.ValueObjects;
using Opas.Shared.Logging;
using Opas.Shared.MultiTenancy;

namespace Opas.Infrastructure.Services;

/// <summary>
/// Merkezi DB'den tenant DB'lerine ürün sync servisi
/// </summary>
public class TenantProductSyncService
{
    private readonly PublicDbContext _publicDb;
    private readonly ControlPlaneDbContext _controlPlaneDb;
    private readonly ILogger<TenantProductSyncService> _logger;
    private readonly IOpasLogger _opasLogger;
    private readonly IServiceProvider _serviceProvider;

    public TenantProductSyncService(
        PublicDbContext publicDb,
        ControlPlaneDbContext controlPlaneDb,
        ILogger<TenantProductSyncService> logger,
        IOpasLogger opasLogger,
        IServiceProvider serviceProvider)
    {
        _publicDb = publicDb;
        _controlPlaneDb = controlPlaneDb;
        _logger = logger;
        _opasLogger = opasLogger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Belirli bir tenant'ın DB'sine merkezi DB'den ürünleri sync et
    /// </summary>
    public async Task<int> SyncProductsToTenantAsync(string tenantId, bool onlyNew = true, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting product sync to tenant {TenantId} (onlyNew: {OnlyNew})", tenantId, onlyNew);
        _opasLogger.LogSystemEvent("TenantProductSync", "Started", new { 
            TenantId = tenantId, 
            OnlyNew = onlyNew,
            Source = "CentralDB"
        });

        try
        {
            // Tenant ID'den GLN çıkar (TNT_229714 → 229714)
            var gln = tenantId.StartsWith("TNT_") ? tenantId.Substring(4) : tenantId;
            
            // GLN Registry'den eczane bilgilerini al
            var pharmacy = await _publicDb.GlnRegistry
                .Where(g => g.Gln == gln && g.Active == true)
                .FirstOrDefaultAsync(ct);

            if (pharmacy == null)
            {
                _logger.LogError("Pharmacy with GLN {Gln} not found or inactive", gln);
                return 0;
            }

            // Connection string'i GLN'den oluştur
            var tenantConnectionString = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";
            
            _logger.LogInformation("Syncing to pharmacy: {PharmacyName} (GLN: {Gln})", 
                pharmacy.CompanyName, pharmacy.Gln);

            // Merkezi DB'den tüm ürünleri al (aktif + pasif)
            // Eczacılar pasif ürünleri de görmeli (stok düşürme, görüntüleme vb.)
            var centralProducts = await _publicDb.CentralProducts
                .ToListAsync(ct);

            if (centralProducts.Count == 0)
            {
                _logger.LogWarning("No products found in central DB");
                return 0;
            }

            // Tenant DB'ye sync et
            using var tenantDb = CreateTenantDbContext(tenantConnectionString);
            
            var syncedCount = await SyncProductsToTenantDb(
                tenantDb, 
                centralProducts, 
                tenantId, 
                onlyNew, 
                ct);

            _logger.LogInformation("Product sync to tenant {TenantId} completed - {Count} products synced", 
                tenantId, syncedCount);

            _opasLogger.LogSystemEvent("TenantProductSync", "Completed", new { 
                TenantId = tenantId,
                SyncedCount = syncedCount,
                Source = "CentralDB"
            });

            return syncedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing products to tenant {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantProductSync", $"Error: {ex.Message}", new { 
                TenantId = tenantId,
                Source = "CentralDB"
            });
            throw;
        }
    }

    /// <summary>
    /// Tüm aktif tenant'lara ürün sync et
    /// </summary>
    public async Task<Dictionary<string, int>> SyncProductsToAllTenantsAsync(bool onlyNew = true, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting product sync to all tenants (onlyNew: {OnlyNew})", onlyNew);
        
        var results = new Dictionary<string, int>();
        
        var activeTenants = await _controlPlaneDb.TenantRecords
            .Where(t => t.Status == "Active")
            .ToListAsync(ct);

        foreach (var tenant in activeTenants)
        {
            try
            {
                var syncedCount = await SyncProductsToTenantAsync(tenant.TenantId, onlyNew, ct);
                results[tenant.TenantId] = syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync products to tenant {TenantId}", tenant.TenantId);
                results[tenant.TenantId] = -1; // Error indicator
            }
        }

        return results;
    }

    private async Task<int> SyncProductsToTenantDb(
        TenantDbContext tenantDb,
        List<CentralProduct> centralProducts,
        string tenantId,
        bool onlyNew,
        CancellationToken ct)
    {
        var syncedCount = 0;
        var syncTime = DateTime.UtcNow;

        // Batch processing
        const int batchSize = 500;
        for (int i = 0; i < centralProducts.Count; i += batchSize)
        {
            var batch = centralProducts.Skip(i).Take(batchSize).ToList();
            var gtins = batch.Select(p => p.Gtin).ToArray();

            // Tenant DB'den mevcut ürünleri al (her durumda gerekli)
            var existingProducts = await tenantDb.Products
                .Where(p => gtins.Contains(p.Gtin))
                .ToDictionaryAsync(p => p.Gtin, ct);

            foreach (var centralProduct in batch)
            {
                if (onlyNew && existingProducts.ContainsKey(centralProduct.Gtin))
                {
                    continue; // Zaten var, skip
                }

                var tenantProduct = existingProducts.GetValueOrDefault(centralProduct.Gtin);
                
                if (tenantProduct == null)
                {
                    // Yeni ürün ekle
                    tenantProduct = new TenantProduct
                    {
                        Gtin = centralProduct.Gtin,
                        DrugName = centralProduct.DrugName,
                        ManufacturerGln = centralProduct.ManufacturerGln,
                        ManufacturerName = centralProduct.ManufacturerName,
                        IsActive = centralProduct.Active,
                        IsImported = centralProduct.Imported
                    };

                    tenantDb.Products.Add(tenantProduct);
                    syncedCount++;
                }
                else if (!onlyNew)
                {
                    // Mevcut ürünü güncelle (ITS verilerini sync et, tenant customization'ları koru)
                    tenantProduct.DrugName = centralProduct.DrugName;
                    tenantProduct.ManufacturerGln = centralProduct.ManufacturerGln;
                    tenantProduct.ManufacturerName = centralProduct.ManufacturerName;
                    tenantProduct.IsActive = centralProduct.Active;
                    tenantProduct.IsImported = centralProduct.Imported;
                    
                    // NOT: Price ve PriceHistory kasıtlı olarak güncellenmedi - tenant'ın customization'ını koru
                    
                    syncedCount++;
                }
            }

            await tenantDb.SaveChangesAsync(ct);
            
            _logger.LogDebug("Processed batch {BatchNumber} for tenant - {Count} products", 
                (i / batchSize) + 1, batch.Count);
        }

        return syncedCount;
    }

    private TenantDbContext CreateTenantDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        var tenantProvider = new SimpleTenantProvider();
        return new TenantDbContext(optionsBuilder.Options, tenantProvider);
    }

    private class SimpleTenantProvider : ITenantProvider
    {
        public string TenantId => "sync-operation";
        public string? ConnectionString => null;
    }
}

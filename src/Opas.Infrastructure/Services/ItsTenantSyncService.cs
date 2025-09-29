using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Opas.Infrastructure.Persistence;
using Opas.Domain.Entities;
using Opas.Domain.ValueObjects;
using Opas.Shared.Logging;
using Opas.Shared.MultiTenancy;

namespace Opas.Infrastructure.Services;

/// <summary>
/// ITS'den alınan ürünleri tenant DB'lerine senkronize eden servis
/// </summary>
public class ItsTenantSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ItsTenantSyncService> _logger;
    private readonly IOpasLogger _opasLogger;
    private readonly ItsProductService _itsProductService;

    public ItsTenantSyncService(
        IServiceProvider serviceProvider,
        ILogger<ItsTenantSyncService> logger,
        IOpasLogger opasLogger,
        ItsProductService itsProductService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _opasLogger = opasLogger;
        _itsProductService = itsProductService;
    }

    /// <summary>
    /// Belirtilen tenant için ITS ürünlerini senkronize et
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="onlyNew">Sadece yeni ürünleri ekle (mevcut customization'lara dokunma)</param>
    /// <param name="ct">Cancellation Token</param>
    public async Task SyncTenantProductsAsync(string tenantId, bool onlyNew = true, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting product sync for tenant {TenantId}, onlyNew={OnlyNew}", tenantId, onlyNew);
        
        _opasLogger.LogSystemEvent("ProductSync", $"Starting sync for tenant {tenantId}", new { 
            TenantId = tenantId, 
            OnlyNew = onlyNew 
        });

        try
        {
            // ITS'den ürün listesi al
            var itsProducts = await _itsProductService.GetProductListAsync(ct);
            if (itsProducts == null || itsProducts.Length == 0)
            {
                _logger.LogWarning("No products received from ITS for tenant {TenantId}", tenantId);
                return;
            }

            // Tenant DB connection string'i al
            var connectionString = await GetTenantConnectionStringAsync(tenantId);
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("No connection string found for tenant {TenantId}", tenantId);
                return;
            }

            // Tenant DB context oluştur
            using var scope = _serviceProvider.CreateScope();
            var tenantDbContext = CreateTenantDbContext(connectionString);

            // Database'in var olduğundan emin ol
            await tenantDbContext.Database.EnsureCreatedAsync(ct);

            var syncStats = new SyncStatistics();

            foreach (var itsProduct in itsProducts)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await ProcessSingleProductAsync(tenantDbContext, itsProduct, onlyNew, syncStats, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing product {Gtin} for tenant {TenantId}", 
                        itsProduct.Gtin, tenantId);
                    syncStats.ErrorCount++;
                }
            }

            // Değişiklikleri kaydet
            await tenantDbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Product sync completed for tenant {TenantId}: {Stats}", 
                tenantId, syncStats);

            _opasLogger.LogSystemEvent("ProductSync", $"Sync completed for tenant {tenantId}", new { 
                TenantId = tenantId,
                Stats = syncStats.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product sync failed for tenant {TenantId}", tenantId);
            
            _opasLogger.LogSystemEvent("ProductSync", $"Sync failed for tenant {tenantId}: {ex.Message}", new { 
                TenantId = tenantId,
                Error = ex.Message
            });
            
            throw;
        }
    }

    /// <summary>
    /// Tüm tenantlar için ürün senkronizasyonu yap
    /// </summary>
    public async Task SyncAllTenantsAsync(bool onlyNew = true, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting product sync for all tenants, onlyNew={OnlyNew}", onlyNew);

        try
        {
            // Tüm aktif tenantları al
            var tenantIds = await GetAllActiveTenantIdsAsync();
            
            _logger.LogInformation("Found {TenantCount} tenants to sync", tenantIds.Count);

            foreach (var tenantId in tenantIds)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await SyncTenantProductsAsync(tenantId, onlyNew, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync tenant {TenantId}, continuing with next", tenantId);
                }

                // Tenantlar arası kısa bekleme
                await Task.Delay(100, ct);
            }

            _opasLogger.LogSystemEvent("ProductSyncAll", "All tenants sync completed", new { 
                TenantCount = tenantIds.Count,
                OnlyNew = onlyNew
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync all tenants");
            throw;
        }
    }

    private async Task ProcessSingleProductAsync(
        TenantDbContext dbContext, 
        ItsProductService.ItsProduct itsProduct, 
        bool onlyNew, 
        SyncStatistics stats,
        CancellationToken ct)
    {
        // GTIN kontrolü
        if (string.IsNullOrEmpty(itsProduct.Gtin))
        {
            stats.SkippedCount++;
            return;
        }

        // Mevcut ürünü kontrol et
        var existingProduct = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Gtin == itsProduct.Gtin, ct);

        if (existingProduct == null)
        {
            // Yeni ürün oluştur
            var newProduct = new TenantProduct
            {
                Gtin = itsProduct.Gtin,
                DrugName = itsProduct.DrugName ?? "Unknown",
                ManufacturerGln = itsProduct.ManufacturerGln ?? "",
                ManufacturerName = itsProduct.ManufacturerName ?? "",
                Price = 0m, // Varsayılan fiyat, tenant değiştirecek
                PriceHistory = new List<PriceHistoryEntry>
                {
                    new PriceHistoryEntry
                    {
                        Price = 0m,
                        Date = DateTime.UtcNow,
                        ChangedBy = "SYSTEM",
                        Reason = "Initial ITS sync",
                        ChangeType = "ITS_SYNC"
                    }
                },
                IsActive = itsProduct.Active == true,
                LastItsSyncAt = DateTime.UtcNow
            };

            dbContext.Products.Add(newProduct);
            stats.AddedCount++;
        }
        else
        {
            // Mevcut ürün güncelleme
            if (onlyNew)
            {
                // Sadece yeni ürünler modunda, mevcut ürünlere dokunma
                stats.SkippedCount++;
                return;
            }

            // Full sync modunda, ITS verilerini güncelle (customization'lar korunur)
            existingProduct.ManufacturerGln = itsProduct.ManufacturerGln ?? existingProduct.ManufacturerGln;
            existingProduct.ManufacturerName = itsProduct.ManufacturerName ?? existingProduct.ManufacturerName;
            existingProduct.IsActive = itsProduct.Active == true;
            existingProduct.LastItsSyncAt = DateTime.UtcNow;

            // İlaç adını sadece özelleştirilmemişse güncelle
            if (existingProduct.DrugName == itsProduct.DrugName || 
                string.IsNullOrEmpty(existingProduct.DrugName))
            {
                existingProduct.DrugName = itsProduct.DrugName ?? existingProduct.DrugName;
            }

            stats.UpdatedCount++;
        }
    }

    private async Task<string?> GetTenantConnectionStringAsync(string tenantId)
    {
        using var scope = _serviceProvider.CreateScope();
        var controlDb = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        
        var tenant = await controlDb.TenantRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId);

        return tenant?.TenantConnectionString;
    }

    private async Task<List<string>> GetAllActiveTenantIdsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var controlDb = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        
        return await controlDb.TenantRecords
            .AsNoTracking()
            .Where(t => t.Status == "Active" || t.Status == "Provisioning")
            .Select(t => t.TenantId)
            .ToListAsync();
    }

    private TenantDbContext CreateTenantDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        // Basit tenant provider oluştur
        var tenantProvider = new SimpleTenantProvider();
        return new TenantDbContext(optionsBuilder.Options, tenantProvider);
    }

    private class SimpleTenantProvider : ITenantProvider
    {
        public string TenantId => "sync-operation";
        public string? ConnectionString => null;
    }

    private class SyncStatistics
    {
        public int AddedCount { get; set; } = 0;
        public int UpdatedCount { get; set; } = 0;
        public int SkippedCount { get; set; } = 0;
        public int ErrorCount { get; set; } = 0;

        public override string ToString() =>
            $"Added: {AddedCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}";
    }
}

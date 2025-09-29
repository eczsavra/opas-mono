using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Opas.Infrastructure.Persistence;
using Opas.Domain.Entities;
using Opas.Domain.ValueObjects;
using Opas.Shared.Logging;

namespace Opas.Infrastructure.Services;

/// <summary>
/// ITS'den ürünleri merkezi DB'ye sync eden servis
/// Bu servis ITS API'sinden ürünleri çeker ve merkezi DB'ye yazar
/// </summary>
public class CentralProductSyncService
{
    private readonly PublicDbContext _publicDb;
    private readonly ItsProductService _itsProductService;
    private readonly ILogger<CentralProductSyncService> _logger;
    private readonly IOpasLogger _opasLogger;

    public CentralProductSyncService(
        PublicDbContext publicDb,
        ItsProductService itsProductService,
        ILogger<CentralProductSyncService> logger,
        IOpasLogger opasLogger)
    {
        _publicDb = publicDb;
        _itsProductService = itsProductService;
        _logger = logger;
        _opasLogger = opasLogger;
    }

    /// <summary>
    /// ITS'den ürünleri çek ve merkezi DB'ye sync et
    /// </summary>
    public async Task<int> SyncProductsFromItsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting central product sync from ITS");
        _opasLogger.LogSystemEvent("CentralProductSync", "Started", new { Source = "ITS" });

        try
        {
            // ITS'den ürünleri çek
            var itsProducts = await _itsProductService.GetProductListAsync(ct);
            if (itsProducts == null || itsProducts.Length == 0)
            {
                _logger.LogWarning("No products received from ITS");
                _opasLogger.LogSystemEvent("CentralProductSync", "No products from ITS", null);
                return 0;
            }

            _logger.LogInformation("Received {Count} products from ITS, syncing to central DB", itsProducts.Length);

            var syncTime = DateTime.UtcNow;
            var addedCount = 0;
            var updatedCount = 0;

            // Batch processing için chunks halinde işle
            const int batchSize = 1; // Tek tek test et
            for (int i = 0; i < itsProducts.Length; i += batchSize)
            {
                var batch = itsProducts.Skip(i).Take(batchSize).ToArray();
                var batchResult = await ProcessBatch(batch, syncTime, ct);
                addedCount += batchResult.Added;
                updatedCount += batchResult.Updated;

                _logger.LogInformation("Processed batch {BatchNumber}/{TotalBatches} - Added: {Added}, Updated: {Updated}",
                    (i / batchSize) + 1, (int)Math.Ceiling((double)itsProducts.Length / batchSize),
                    batchResult.Added, batchResult.Updated);
            }

            _logger.LogInformation("Central product sync completed - Added: {Added}, Updated: {Updated}, Total: {Total}",
                addedCount, updatedCount, itsProducts.Length);

            _opasLogger.LogSystemEvent("CentralProductSync", "Completed", new {
                Source = "ITS",
                TotalProducts = itsProducts.Length,
                AddedCount = addedCount,
                UpdatedCount = updatedCount
            });

            return addedCount + updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during central product sync from ITS");
            _opasLogger.LogSystemEvent("CentralProductSync", $"Error: {ex.Message}", new { Source = "ITS" });
            throw;
        }
    }

    private async Task<(int Added, int Updated)> ProcessBatch(
        ItsProductService.ItsProduct[] batch, 
        DateTime syncTime, 
        CancellationToken ct)
    {
        // GTIN'leri unique yap - aynı GTIN'den sadece bir tane işle
        var uniqueProducts = batch
            .Where(p => !string.IsNullOrEmpty(p.Gtin))
            .GroupBy(p => p.Gtin)
            .Select(g => g.First()) // Her GTIN'den sadece ilkini al
            .ToArray();
        
        var gtins = uniqueProducts.Select(p => p.Gtin!).ToArray();
        
        _logger.LogInformation("Processing batch: Original={OriginalCount}, Unique={UniqueCount}", 
            batch.Length, uniqueProducts.Length);
        
        // Mevcut ürünleri çek
        var existingProducts = await _publicDb.CentralProducts
            .Where(p => gtins.Contains(p.Gtin))
            .ToDictionaryAsync(p => p.Gtin, ct);

        var addedCount = 0;
        var updatedCount = 0;

        foreach (var itsProduct in uniqueProducts)
        {
            if (string.IsNullOrEmpty(itsProduct.Gtin))
                continue;

            var rawData = JsonSerializer.Serialize(itsProduct);

            if (existingProducts.TryGetValue(itsProduct.Gtin, out var existingProduct))
            {
                // Güncelle
                existingProduct.DrugName = itsProduct.DrugName ?? existingProduct.DrugName;
                existingProduct.ManufacturerGln = itsProduct.ManufacturerGln;
                existingProduct.ManufacturerName = itsProduct.ManufacturerName;
                existingProduct.Active = itsProduct.Active;
                existingProduct.Imported = itsProduct.Imported;
                existingProduct.MarkUpdated();

                updatedCount++;
            }
            else
            {
                // Yeni ürün ekle
                var centralProduct = new CentralProduct
                {
                    Gtin = itsProduct.Gtin,
                    DrugName = itsProduct.DrugName ?? "Unknown",
                    ManufacturerGln = itsProduct.ManufacturerGln,
                    ManufacturerName = itsProduct.ManufacturerName,
                    Active = itsProduct.Active,
                    Imported = itsProduct.Imported
                };

                _publicDb.CentralProducts.Add(centralProduct);
                addedCount++;
            }
        }

        try
        {
            await _publicDb.SaveChangesAsync(ct);
            return (addedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save central products batch");
            _opasLogger.LogSystemEvent("CentralProductSync", $"Batch save error: {ex.Message}", new { BatchSize = batch.Length });
            throw;
        }
    }

    /// <summary>
    /// Merkezi DB'deki toplam ürün sayısını getir
    /// </summary>
    public async Task<int> GetCentralProductCountAsync(CancellationToken ct = default)
    {
        return await _publicDb.CentralProducts.CountAsync(ct);
    }

    /// <summary>
    /// Merkezi DB'deki aktif ürün sayısını getir
    /// </summary>
    public async Task<int> GetActiveProductCountAsync(CancellationToken ct = default)
    {
        return await _publicDb.CentralProducts.Where(p => p.Active).CountAsync(ct);
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Opas.Infrastructure.Persistence;
using Opas.Shared.Logging;
using Opas.Shared.MultiTenancy;

namespace Opas.Infrastructure.Services;

/// <summary>
/// Merkezi DB'den tenant DB'lerine GLN sync servisi
/// Eczacılar diğer paydaşları (eczane, tedarikçi, vb.) görebilir
/// </summary>
public class TenantGlnSyncService
{
    private readonly ILogger<TenantGlnSyncService> _logger;
    private readonly IOpasLogger _opasLogger;
    private readonly PublicDbContext _publicDb;
    private readonly ControlPlaneDbContext _controlPlaneDb;
    private readonly IServiceProvider _serviceProvider;

    public TenantGlnSyncService(
        ILogger<TenantGlnSyncService> logger,
        IOpasLogger opasLogger,
        PublicDbContext publicDb,
        ControlPlaneDbContext controlPlaneDb,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _opasLogger = opasLogger;
        _publicDb = publicDb;
        _controlPlaneDb = controlPlaneDb;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Belirli bir tenant'a GLN listesini sync et
    /// </summary>
    public async Task<int> SyncGlnsToTenantAsync(string tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting GLN sync to tenant {TenantId}", tenantId);
        _opasLogger.LogSystemEvent("TenantGlnSync", "Started", new { TenantId = tenantId, Source = "CentralDB" });

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

            _logger.LogInformation("Syncing to pharmacy: {PharmacyName} (GLN: {Gln})", 
                pharmacy.CompanyName, pharmacy.Gln);

            // Connection string'i GLN'den oluştur
            var tenantConnectionString = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{gln};Username=postgres;Password=postgres";

            // Merkezi DB'den tüm GLN'leri al
            var centralGlns = await _publicDb.GlnRegistry
                .ToListAsync(ct);

            return await SyncGlnsToTenantDb(tenantConnectionString, centralGlns, tenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing GLNs to tenant {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantGlnSync", "Error", new 
            { 
                TenantId = tenantId, 
                Error = ex.Message 
            });
            return 0;
        }
    }

    /// <summary>
    /// Tüm tenant'lara GLN sync et
    /// </summary>
    public async Task<Dictionary<string, int>> SyncGlnsToAllTenantsAsync(CancellationToken ct = default)
    {
        var results = new Dictionary<string, int>();

        // Aktif eczaneleri al
        var pharmacies = await _publicDb.GlnRegistry
            .Where(g => g.Active == true)
            .ToListAsync(ct);

        foreach (var pharmacy in pharmacies)
        {
            try
            {
                var tenantId = $"TNT_{pharmacy.Gln}";
                var syncedCount = await SyncGlnsToTenantAsync(tenantId, ct);
                results[tenantId] = syncedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync GLNs to pharmacy {Gln}", pharmacy.Gln);
                results[$"TNT_{pharmacy.Gln}"] = -1; // Error indicator
            }
        }

        return results;
    }

    /// <summary>
    /// Belirli tenant DB'ye GLN'leri sync et
    /// </summary>
    private async Task<int> SyncGlnsToTenantDb(
        string connectionString, 
        List<Opas.Shared.ControlPlane.GlnRecord> centralGlns,
        string tenantId,
        CancellationToken ct = default)
    {
        int syncedCount = 0;

        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        // Mevcut GLN'leri al
        var existingGlns = new HashSet<string>();
        using (var selectCmd = new NpgsqlCommand(@"
            SELECT gln FROM gln_registry", connection))
        {
            using var reader = await selectCmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                existingGlns.Add(reader.GetString(0));
            }
        }

        // Batch processing
        const int batchSize = 100;
        for (int i = 0; i < centralGlns.Count; i += batchSize)
        {
            var batch = centralGlns.Skip(i).Take(batchSize).ToList();

            using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                foreach (var centralGln in batch)
                {
                    if (existingGlns.Contains(centralGln.Gln))
                    {
                        // Update existing GLN
                        using var updateCmd = new NpgsqlCommand(@"
                            UPDATE gln_registry SET 
                                company_name = @company_name,
                                authorized = @authorized,
                                email = @email,
                                phone = @phone,
                                city = @city,
                                town = @town,
                                address = @address,
                                active = @active,
                                imported_at_utc = NOW()
                            WHERE gln = @gln", connection, transaction);

                        updateCmd.Parameters.AddWithValue("@gln", centralGln.Gln);
                        updateCmd.Parameters.AddWithValue("@company_name", centralGln.CompanyName ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@authorized", centralGln.Authorized ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@email", centralGln.Email ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@phone", centralGln.Phone ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@city", centralGln.City ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@town", centralGln.Town ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@address", centralGln.Address ?? (object)DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@active", centralGln.Active ?? true);

                        await updateCmd.ExecuteNonQueryAsync(ct);
                    }
                    else
                    {
                        // Insert new GLN
                        using var insertCmd = new NpgsqlCommand(@"
                            INSERT INTO gln_registry (
                                gln, company_name, authorized, email, phone, 
                                city, town, address, active, source, imported_at_utc
                            ) VALUES (
                                @gln, @company_name, @authorized, @email, @phone,
                                @city, @town, @address, @active, 'central_sync', NOW()
                            )", connection, transaction);

                        insertCmd.Parameters.AddWithValue("@gln", centralGln.Gln);
                        insertCmd.Parameters.AddWithValue("@company_name", centralGln.CompanyName ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@authorized", centralGln.Authorized ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@email", centralGln.Email ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@phone", centralGln.Phone ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@city", centralGln.City ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@town", centralGln.Town ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@address", centralGln.Address ?? (object)DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@active", centralGln.Active ?? true);

                        await insertCmd.ExecuteNonQueryAsync(ct);
                        existingGlns.Add(centralGln.Gln); // Cache'e ekle
                    }

                    syncedCount++;
                }

                await transaction.CommitAsync(ct);
                _logger.LogDebug("GLN sync batch completed - {Count} GLNs processed", batch.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "GLN sync batch failed for tenant {TenantId}", tenantId);
                throw;
            }
        }

        _logger.LogInformation("GLN sync to tenant {TenantId} completed - {Count} GLNs synced", 
            tenantId, syncedCount);
        _opasLogger.LogSystemEvent("TenantGlnSync", "Completed", new 
        { 
            TenantId = tenantId, 
            SyncedCount = syncedCount, 
            Source = "CentralDB" 
        });

        return syncedCount;
    }
}

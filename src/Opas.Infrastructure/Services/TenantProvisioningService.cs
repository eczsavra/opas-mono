using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Opas.Infrastructure.Persistence;
using Opas.Shared.ControlPlane;
using Opas.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Opas.Infrastructure.Services;

public class TenantProvisioningService
{
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly IOpasLogger _opasLogger;
    private readonly ControlPlaneDbContext _controlPlaneDb;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public TenantProvisioningService(
        ILogger<TenantProvisioningService> logger,
        IOpasLogger opasLogger,
        ControlPlaneDbContext controlPlaneDb,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _opasLogger = opasLogger;
        _controlPlaneDb = controlPlaneDb;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool Success, string ConnectionString, string Message)> ProvisionTenantDatabaseAsync(
        string tenantId, 
        string pharmacistGln,
        string pharmacyName,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting tenant provisioning for {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantProvisioning", "Started", new { TenantId = tenantId, Gln = pharmacistGln });

            // 1. Database adını oluştur (TNT_GLN → opas_tenant_GLN)
            var dbName = $"opas_tenant_{tenantId.Replace("TNT_", "").ToLower()}";
            
            // 2. Master connection string'i al
            var masterConnStr = _configuration.GetConnectionString("ControlPlane") 
                ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;";

            // 3. Yeni DB oluştur
            using (var conn = new NpgsqlConnection(masterConnStr))
            {
                await conn.OpenAsync(ct);
                
                // DB var mı kontrol et
                using (var checkCmd = new NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{dbName}'", conn))
                {
                    var exists = await checkCmd.ExecuteScalarAsync(ct);
                    if (exists != null)
                    {
                        _logger.LogWarning("Database {DbName} already exists", dbName);
                        // Varsa connection string döndür
                        var existingConnStr = masterConnStr.Replace("Database=postgres", $"Database={dbName}");
                        return (true, existingConnStr, "Database already exists");
                    }
                }

                // DB oluştur
                using (var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\" WITH ENCODING='UTF8'", conn))
                {
                    await createCmd.ExecuteNonQueryAsync(ct);
                }
                _logger.LogInformation("Created database {DbName}", dbName);
            }

            // 4. Yeni DB'ye bağlan ve tabloları oluştur
            var tenantConnStr = masterConnStr.Replace("Database=postgres", $"Database={dbName}");
            
            using (var conn = new NpgsqlConnection(tenantConnStr))
            {
                await conn.OpenAsync(ct);
                
                // Tenant tablolarını oluştur - sadece 3 tablo
                var createTablesSql = @"
                    -- Products tablosu (central_products'tan beslenir)
                    CREATE TABLE IF NOT EXISTS products (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        gtin VARCHAR(50) UNIQUE NOT NULL,
                        drug_name VARCHAR(500) NOT NULL,
                        manufacturer_gln VARCHAR(50),
                        manufacturer_name VARCHAR(500),
                        price DECIMAL(18,4) DEFAULT 0,
                        price_history JSONB DEFAULT '[]'::jsonb,
                        is_active BOOLEAN DEFAULT TRUE,
                        last_its_sync_at TIMESTAMP WITH TIME ZONE,
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );
                    CREATE INDEX idx_products_gtin ON products(gtin);
                    CREATE INDEX idx_products_active ON products(is_active) WHERE is_active = TRUE;

                    -- GLN List tablosu (gln_registry'den beslenir. Paydaş bilgileri - merkezi DB'den sync)
                    CREATE TABLE IF NOT EXISTS gln_list (
                        id SERIAL PRIMARY KEY,
                        gln VARCHAR(50) UNIQUE NOT NULL,
                        company_name VARCHAR(500),
                        authorized VARCHAR(200),
                        email VARCHAR(200),
                        phone VARCHAR(50),
                        city VARCHAR(100),
                        town VARCHAR(100),
                        address TEXT,
                        active BOOLEAN DEFAULT TRUE,
                        source VARCHAR(50) DEFAULT 'central_sync',
                        imported_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                    );
                    CREATE INDEX idx_gln_list_gln ON gln_list(gln);
                    CREATE INDEX idx_gln_list_city ON gln_list(city);
                    CREATE INDEX idx_gln_list_active ON gln_list(active);

                    -- Tenant Info tablosu (tenants tablosundan beslenir)
                    CREATE TABLE IF NOT EXISTS tenant_info (
                        t_id VARCHAR(50) PRIMARY KEY,
                        gln VARCHAR(13) NOT NULL,
                        type VARCHAR(20) NOT NULL DEFAULT 'eczane',
                        eczane_adi VARCHAR(200),
                        ili VARCHAR(100),
                        ilcesi VARCHAR(100),
                        isactive BOOLEAN DEFAULT TRUE,
                        ad VARCHAR(100) NOT NULL,
                        soyad VARCHAR(100) NOT NULL,
                        tc_no VARCHAR(11),
                        dogum_yili INTEGER,
                        isnviverified BOOLEAN DEFAULT FALSE,
                        email VARCHAR(200) NOT NULL,
                        isemailverified BOOLEAN DEFAULT FALSE,
                        cep_tel VARCHAR(15),
                        isceptelverified BOOLEAN DEFAULT FALSE,
                        username VARCHAR(50) NOT NULL,
                        password VARCHAR(255) NOT NULL,
                        iscompleted BOOLEAN DEFAULT FALSE,
                        kayit_olusturulma_zamani TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        kayit_guncellenme_zamani TIMESTAMP WITH TIME ZONE,
                        kayit_silinme_zamani TIMESTAMP WITH TIME ZONE
                    );
                    CREATE INDEX idx_tenant_info_gln ON tenant_info(gln);
                    CREATE INDEX idx_tenant_info_username ON tenant_info(username);
                    CREATE INDEX idx_tenant_info_email ON tenant_info(email);
                ";

                using (var cmd = new NpgsqlCommand(createTablesSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                _logger.LogInformation("Created tables for tenant {TenantId}", tenantId);
            }

            // 5. Control Plane'e tenant kaydını güncelle
            var tenant = await _controlPlaneDb.Tenants
                .FirstOrDefaultAsync(t => t.TId == tenantId, ct);
                
            if (tenant == null)
            {
                _logger.LogError("Tenant {TenantId} not found in database after creation", tenantId);
                return (false, "", $"Tenant {tenantId} not found");
            }

            _logger.LogInformation("Tenant {TenantId} found in database", tenantId);

            // 6. OTOMATIK ÜRÜN SYNC - Merkezi DB'den yeni tenant'a ürünleri kopyala
            _logger.LogInformation("Starting product sync for new tenant {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantProvisioning", "Starting product sync", new { TenantId = tenantId });

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var productSyncService = scope.ServiceProvider.GetRequiredService<TenantProductSyncService>();
                
                var syncedCount = await productSyncService.SyncProductsToTenantAsync(tenantId, onlyNew: false, ct);
                
                _logger.LogInformation("Product sync completed for new tenant {TenantId} - {Count} products synced", 
                    tenantId, syncedCount);
                _opasLogger.LogSystemEvent("TenantProvisioning", "Product sync completed", new { 
                    TenantId = tenantId, 
                    SyncedProductCount = syncedCount 
                });
            }
            catch (Exception syncEx)
            {
                _logger.LogError(syncEx, "Product sync failed for new tenant {TenantId} - continuing anyway", tenantId);
                _opasLogger.LogSystemEvent("TenantProvisioning", "Product sync failed", new { 
                    TenantId = tenantId, 
                    Error = syncEx.Message 
                });
                // Sync hatası tenant'ı durdurmaz, devam et
            }

            // 7. OTOMATIK GLN SYNC - Merkezi DB'den yeni tenant'a GLN'leri kopyala
            _logger.LogInformation("Starting GLN sync for new tenant {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantProvisioning", "Starting GLN sync", new { TenantId = tenantId });

            try
            {
                using var scope2 = _serviceProvider.CreateScope();
                var glnSyncService = scope2.ServiceProvider.GetRequiredService<TenantGlnSyncService>();
                
                var syncedGlnCount = await glnSyncService.SyncGlnsToTenantAsync(tenantId, ct);
                
                _logger.LogInformation("GLN sync completed for new tenant {TenantId} - {Count} GLNs synced", 
                    tenantId, syncedGlnCount);
                _opasLogger.LogSystemEvent("TenantProvisioning", "GLN sync completed", new { 
                    TenantId = tenantId, 
                    SyncedGlnCount = syncedGlnCount 
                });
            }
            catch (Exception glnSyncEx)
            {
                _logger.LogError(glnSyncEx, "GLN sync failed for new tenant {TenantId} - continuing anyway", tenantId);
                _opasLogger.LogSystemEvent("TenantProvisioning", "GLN sync failed", new { 
                    TenantId = tenantId, 
                    Error = glnSyncEx.Message 
                });
                // GLN sync hatası da tenant'ı durdurmaz, devam et
            }

            _logger.LogInformation("Successfully provisioned tenant {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantProvisioning", "Completed", new { 
                TenantId = tenantId, 
                DbName = dbName,
                Gln = pharmacistGln 
            });

            return (true, tenantConnStr, "Tenant database provisioned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning tenant {TenantId}", tenantId);
            _opasLogger.LogSystemEvent("TenantProvisioning", "Failed", new { 
                TenantId = tenantId, 
                Error = ex.Message 
            });
            return (false, string.Empty, $"Provisioning failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sync tenant_info to tenant database with updated isCompleted flag
    /// </summary>
    public async Task<bool> SyncTenantInfoAsync(string tenantId, CancellationToken ct = default)
    {
        try
        {
            // Get updated tenant from database
            var tenant = await _controlPlaneDb.Tenants
                .FirstOrDefaultAsync(t => t.TId == tenantId, ct);

            if (tenant == null)
            {
                _logger.LogError("Tenant {TenantId} not found for tenant_info sync", tenantId);
                return false;
            }

            // Build connection string
            var dbSuffix = tenantId.StartsWith("TNT_") ? tenantId.Substring(4) : tenantId;
            var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{dbSuffix};Username=postgres;Password=postgres";

            using (var tenantConn = new NpgsqlConnection(tenantConnStr))
            {
                await tenantConn.OpenAsync(ct);
                
                var upsertTenantInfoSql = @"
                    INSERT INTO tenant_info (
                        t_id, gln, type, eczane_adi, ili, ilcesi, isactive,
                        ad, soyad, tc_no, dogum_yili, isnviverified,
                        email, isemailverified, cep_tel, isceptelverified,
                        username, password, iscompleted,
                        kayit_olusturulma_zamani, kayit_guncellenme_zamani, kayit_silinme_zamani
                    ) VALUES (
                        @t_id, @gln, @type, @eczane_adi, @ili, @ilcesi, @isactive,
                        @ad, @soyad, @tc_no, @dogum_yili, @isnviverified,
                        @email, @isemailverified, @cep_tel, @isceptelverified,
                        @username, @password, @iscompleted,
                        @kayit_olusturulma_zamani, @kayit_guncellenme_zamani, @kayit_silinme_zamani
                    )
                    ON CONFLICT (t_id) DO UPDATE SET
                        iscompleted = EXCLUDED.iscompleted,
                        kayit_guncellenme_zamani = EXCLUDED.kayit_guncellenme_zamani";
                
                using (var cmd = new NpgsqlCommand(upsertTenantInfoSql, tenantConn))
                {
                    cmd.Parameters.AddWithValue("@t_id", tenant.TId);
                    cmd.Parameters.AddWithValue("@gln", tenant.Gln);
                    cmd.Parameters.AddWithValue("@type", tenant.Type);
                    cmd.Parameters.AddWithValue("@eczane_adi", (object?)tenant.EczaneAdi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ili", (object?)tenant.Ili ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ilcesi", (object?)tenant.Ilcesi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@isactive", tenant.IsActive);
                    cmd.Parameters.AddWithValue("@ad", tenant.Ad);
                    cmd.Parameters.AddWithValue("@soyad", tenant.Soyad);
                    cmd.Parameters.AddWithValue("@tc_no", (object?)tenant.TcNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dogum_yili", tenant.DogumYili.HasValue ? (object)tenant.DogumYili.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@isnviverified", tenant.IsNviVerified);
                    cmd.Parameters.AddWithValue("@email", tenant.Email);
                    cmd.Parameters.AddWithValue("@isemailverified", tenant.IsEmailVerified);
                    cmd.Parameters.AddWithValue("@cep_tel", (object?)tenant.CepTel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@isceptelverified", tenant.IsCepTelVerified);
                    cmd.Parameters.AddWithValue("@username", tenant.Username);
                    cmd.Parameters.AddWithValue("@password", tenant.Password);
                    cmd.Parameters.AddWithValue("@iscompleted", tenant.IsCompleted);
                    cmd.Parameters.AddWithValue("@kayit_olusturulma_zamani", tenant.KayitOlusturulmaZamani);
                    cmd.Parameters.AddWithValue("@kayit_guncellenme_zamani", tenant.KayitGuncellenmeZamani.HasValue ? (object)tenant.KayitGuncellenmeZamani.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@kayit_silinme_zamani", tenant.KayitSilinmeZamani.HasValue ? (object)tenant.KayitSilinmeZamani.Value : DBNull.Value);
                    
                    await cmd.ExecuteNonQueryAsync(ct);
                }
            }
            
            _logger.LogInformation("Tenant info synced with isCompleted={IsCompleted} for {TenantId}", 
                tenant.IsCompleted, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync tenant info for {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> ValidateTenantDatabaseAsync(string tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _controlPlaneDb.Tenants
                .FirstOrDefaultAsync(t => t.TId == tenantId, ct);
                
            if (tenant == null)
                return false;

            // Build connection string for tenant database (TNT_GLN → opas_tenant_GLN)
            var tenantConnStr = $"Host=127.0.0.1;Port=5432;Database=opas_tenant_{tenantId.Replace("TNT_", "").ToLower()};Username=postgres;Password=postgres";
            using (var conn = new NpgsqlConnection(tenantConnStr))
            {
                await conn.OpenAsync(ct);
                
                // Tabloları kontrol et
                using (var cmd = new NpgsqlCommand(@"
                    SELECT COUNT(*) FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name IN ('products', 'customers', 'sales', 'stocks')", conn))
                {
                    var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
                    return tableCount >= 4;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating tenant database for {TenantId}", tenantId);
            return false;
        }
    }
}

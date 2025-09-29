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

            // 1. Database adını oluştur
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
                
                // Tenant tablolarını oluştur
                var createTablesSql = @"
                    -- Customers tablosu
                    CREATE TABLE IF NOT EXISTS customers (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tc_no VARCHAR(11) UNIQUE NOT NULL,
                        first_name VARCHAR(100) NOT NULL,
                        last_name VARCHAR(100) NOT NULL,
                        phone VARCHAR(20),
                        email VARCHAR(200),
                        address TEXT,
                        city VARCHAR(100),
                        district VARCHAR(100),
                        birth_date DATE,
                        gender VARCHAR(10),
                        notes TEXT,
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );

                    -- Products tablosu (tenant-specific customizations)
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

                    -- GLN Registry tablosu (paydaş bilgileri - merkezi DB'den sync)
                    CREATE TABLE IF NOT EXISTS gln_registry (
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
                    CREATE INDEX idx_gln_registry_gln ON gln_registry(gln);
                    CREATE INDEX idx_gln_registry_city ON gln_registry(city);
                    CREATE INDEX idx_gln_registry_active ON gln_registry(active);

                    -- Stocks tablosu
                    CREATE TABLE IF NOT EXISTS stocks (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        product_id UUID NOT NULL REFERENCES products(id),
                        quantity INTEGER DEFAULT 0,
                        min_quantity INTEGER DEFAULT 5,
                        max_quantity INTEGER DEFAULT 100,
                        location VARCHAR(100),
                        batch_no VARCHAR(100),
                        expiry_date DATE,
                        last_purchase_price DECIMAL(18,4),
                        last_sale_price DECIMAL(18,4),
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );
                    CREATE INDEX idx_stocks_product ON stocks(product_id);

                    -- Sales tablosu
                    CREATE TABLE IF NOT EXISTS sales (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        sale_no VARCHAR(50) UNIQUE NOT NULL,
                        customer_id UUID REFERENCES customers(id),
                        sale_date TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        payment_method VARCHAR(50) NOT NULL, -- CASH, CARD, CREDIT, IBAN, QR, EMANET
                        total_amount DECIMAL(18,4) NOT NULL,
                        discount_amount DECIMAL(18,4) DEFAULT 0,
                        tax_amount DECIMAL(18,4) DEFAULT 0,
                        net_amount DECIMAL(18,4) NOT NULL,
                        status VARCHAR(50) DEFAULT 'COMPLETED', -- COMPLETED, CANCELLED, RETURNED
                        notes TEXT,
                        yn_okc_receipt_no VARCHAR(100),
                        yn_okc_sync_status VARCHAR(50),
                        yn_okc_sync_at TIMESTAMP WITH TIME ZONE,
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );
                    CREATE INDEX idx_sales_date ON sales(sale_date);
                    CREATE INDEX idx_sales_customer ON sales(customer_id);

                    -- Sale Items tablosu
                    CREATE TABLE IF NOT EXISTS sale_items (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        sale_id UUID NOT NULL REFERENCES sales(id),
                        product_id UUID NOT NULL REFERENCES products(id),
                        quantity INTEGER NOT NULL,
                        unit_price DECIMAL(18,4) NOT NULL,
                        discount_amount DECIMAL(18,4) DEFAULT 0,
                        tax_rate DECIMAL(5,2) DEFAULT 18,
                        tax_amount DECIMAL(18,4) NOT NULL,
                        total_amount DECIMAL(18,4) NOT NULL,
                        batch_no VARCHAR(100),
                        expiry_date DATE,
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );
                    CREATE INDEX idx_sale_items_sale ON sale_items(sale_id);
                    CREATE INDEX idx_sale_items_product ON sale_items(product_id);

                    -- Prescriptions tablosu
                    CREATE TABLE IF NOT EXISTS prescriptions (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        prescription_no VARCHAR(100) UNIQUE NOT NULL,
                        customer_id UUID REFERENCES customers(id),
                        doctor_name VARCHAR(200),
                        doctor_tc VARCHAR(11),
                        hospital_name VARCHAR(300),
                        prescription_date DATE,
                        validity_date DATE,
                        status VARCHAR(50) DEFAULT 'ACTIVE',
                        notes TEXT,
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );
                    CREATE INDEX idx_prescriptions_customer ON prescriptions(customer_id);

                    -- Prescription Items tablosu
                    CREATE TABLE IF NOT EXISTS prescription_items (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        prescription_id UUID NOT NULL REFERENCES prescriptions(id),
                        product_id UUID REFERENCES products(id),
                        drug_name VARCHAR(500) NOT NULL,
                        quantity INTEGER NOT NULL,
                        usage_instructions TEXT,
                        is_dispensed BOOLEAN DEFAULT FALSE,
                        dispensed_at TIMESTAMP WITH TIME ZONE,
                        is_deleted BOOLEAN DEFAULT FALSE,
                        created_at_utc TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at_utc TIMESTAMP WITH TIME ZONE,
                        created_by VARCHAR(100),
                        updated_by VARCHAR(100)
                    );
                    CREATE INDEX idx_prescription_items_prescription ON prescription_items(prescription_id);
                ";

                using (var cmd = new NpgsqlCommand(createTablesSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                _logger.LogInformation("Created tables for tenant {TenantId}", tenantId);
            }

            // 5. Control Plane'e tenant kaydını güncelle
            var tenant = await _controlPlaneDb.TenantRecords
                .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
                
            if (tenant != null)
            {
                tenant.TenantConnectionString = tenantConnStr;
                tenant.Status = "Active";
                await _controlPlaneDb.SaveChangesAsync(ct);
            }

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

    public async Task<bool> ValidateTenantDatabaseAsync(string tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _controlPlaneDb.TenantRecords
                .FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
                
            if (tenant == null || string.IsNullOrEmpty(tenant.TenantConnectionString))
                return false;

            using (var conn = new NpgsqlConnection(tenant.TenantConnectionString))
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

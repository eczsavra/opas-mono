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
                
                // Tenant tablolarını oluştur
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
                        updated_by VARCHAR(100),
                        category VARCHAR(50) DEFAULT 'DRUG' CHECK (category IN ('DRUG', 'NON_DRUG')),
                        has_datamatrix BOOLEAN DEFAULT FALSE,
                        requires_expiry_tracking BOOLEAN DEFAULT TRUE,
                        is_controlled BOOLEAN DEFAULT FALSE
                    );
                    CREATE INDEX idx_products_gtin ON products(gtin);
                    CREATE INDEX idx_products_active ON products(is_active) WHERE is_active = TRUE;
                    CREATE INDEX idx_products_category ON products(category);
                    CREATE INDEX idx_products_datamatrix ON products(has_datamatrix) WHERE has_datamatrix = TRUE;

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

                    -- Draft Sales tablosu (tamamlanmamış satışlar)
                    CREATE TABLE IF NOT EXISTS draft_sales (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tab_id VARCHAR(50) NOT NULL UNIQUE,
                        tab_label VARCHAR(100) NOT NULL,
                        products JSONB NOT NULL DEFAULT '[]'::jsonb,
                        is_completed BOOLEAN DEFAULT FALSE,
                        created_by VARCHAR(100) NOT NULL,
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        completed_at TIMESTAMP WITH TIME ZONE,
                        display_order INT DEFAULT 0
                    );
                    CREATE INDEX idx_draft_sales_tab_id ON draft_sales(tab_id);
                    CREATE INDEX idx_draft_sales_completed ON draft_sales(is_completed) WHERE is_completed = FALSE;
                    CREATE INDEX idx_draft_sales_created_by ON draft_sales(created_by);
                    CREATE INDEX idx_draft_sales_display_order ON draft_sales(display_order) WHERE is_completed = FALSE;

                    -- ========================================
                    -- STOK MODÜLÜ TABLOLARI
                    -- ========================================

                    -- Hareket Tipleri (Referans Tablo)
                    CREATE TABLE IF NOT EXISTS movement_types (
                        code VARCHAR(50) PRIMARY KEY,
                        name VARCHAR(200) NOT NULL,
                        direction VARCHAR(10) NOT NULL CHECK (direction IN ('IN', 'OUT')),
                        category VARCHAR(50) NOT NULL,
                        is_active BOOLEAN DEFAULT TRUE,
                        description TEXT,
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                    );

                    -- Başlangıç verileri
                    INSERT INTO movement_types (code, name, direction, category, description) VALUES
                        ('SALE_RETAIL', 'Perakende Satış', 'OUT', 'SALE', 'Normal müşteri satışı'),
                        ('SALE_PRESCRIPTION', 'Reçeteli Satış', 'OUT', 'SALE', 'SGK/Medula reçeteli satış'),
                        ('SALE_INSURANCE', 'Özel Sigorta Satışı', 'OUT', 'SALE', 'Özel sağlık sigortası satışı'),
                        ('SALE_CONSIGNMENT', 'Emanet Satış', 'OUT', 'SALE', 'Reçete sonra getirilecek'),
                        ('SALE_CREDIT', 'Veresiye Satış', 'OUT', 'SALE', 'Borç, sonra ödenecek'),
                        ('PURCHASE_DEPOT', 'Depodan Alış', 'IN', 'PURCHASE', 'Normal toptan alış'),
                        ('PURCHASE_MARKETPLACE', 'Pazaryeri Alışı', 'IN', 'PURCHASE', 'Online platform alışı'),
                        ('PURCHASE_OTHER', 'Diğer Alış', 'IN', 'PURCHASE', 'Depo dışı alış'),
                        ('CUSTOMER_RETURN', 'Müşteri İadesi', 'IN', 'RETURN', 'Müşteriden ürün iadesi'),
                        ('RETURN_TO_DEPOT', 'Depoya İade', 'OUT', 'RETURN', 'Depoya ürün iadesi'),
                        ('EXCHANGE_IN', 'Takas Alış', 'IN', 'EXCHANGE', 'Başka eczacıdan alış'),
                        ('EXCHANGE_OUT', 'Takas Satış', 'OUT', 'EXCHANGE', 'Başka eczacıya satış'),
                        ('WASTE', 'Zayi/Fire', 'OUT', 'LOSS', 'Kırılma, SKT dolmuş, vb.'),
                        ('UNKNOWN_IN', 'Sebebi Bilinmeyen Giriş', 'IN', 'OTHER', 'Müşteri getirdi, hediye, vb.'),
                        ('CORRECTION', 'Manuel Düzeltme', 'IN', 'OTHER', 'Stok düzeltmesi')
                    ON CONFLICT (code) DO NOTHING;

                    -- Lokasyonlar (Raf/Dolap Takibi)
                    CREATE TABLE IF NOT EXISTS storage_locations (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        location_code VARCHAR(50) UNIQUE NOT NULL,
                        location_name VARCHAR(200) NOT NULL,
                        location_type VARCHAR(50) NOT NULL CHECK (location_type IN ('SHELF', 'CABINET', 'REFRIGERATOR', 'VAULT', 'OTHER')),
                        parent_location_id UUID REFERENCES storage_locations(id),
                        temperature_controlled BOOLEAN DEFAULT FALSE,
                        prescription_only BOOLEAN DEFAULT FALSE,
                        is_active BOOLEAN DEFAULT TRUE,
                        notes TEXT,
                        created_by VARCHAR(100),
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                    );
                    CREATE INDEX idx_storage_locations_code ON storage_locations(location_code);
                    CREATE INDEX idx_storage_locations_active ON storage_locations(is_active) WHERE is_active = TRUE;

                    -- Stok Hareketleri (DEFTER - EN ÖNEMLİ!)
                    CREATE TABLE IF NOT EXISTS stock_movements (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        movement_number VARCHAR(50) UNIQUE NOT NULL,
                        movement_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        movement_type VARCHAR(50) NOT NULL REFERENCES movement_types(code),
                        product_id UUID NOT NULL,
                        quantity_change INT NOT NULL,
                        unit_cost DECIMAL(18,4),
                        total_cost DECIMAL(18,2),
                        serial_number VARCHAR(100),
                        lot_number VARCHAR(100),
                        expiry_date DATE,
                        gtin VARCHAR(50),
                        batch_id UUID,
                        reference_type VARCHAR(50),
                        reference_id VARCHAR(100),
                        location_id UUID REFERENCES storage_locations(id),
                        created_by VARCHAR(100) NOT NULL,
                        created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                        is_correction BOOLEAN DEFAULT FALSE,
                        correction_reason TEXT,
                        notes TEXT
                    );
                    CREATE INDEX idx_stock_movements_product ON stock_movements(product_id);
                    CREATE INDEX idx_stock_movements_date ON stock_movements(movement_date DESC);
                    CREATE INDEX idx_stock_movements_type ON stock_movements(movement_type);
                    CREATE INDEX idx_stock_movements_serial ON stock_movements(serial_number) WHERE serial_number IS NOT NULL;
                    CREATE INDEX idx_stock_movements_created_by ON stock_movements(created_by);
                    CREATE INDEX idx_stock_movements_reference ON stock_movements(reference_type, reference_id);

                    -- İlaç Stok Detayı (Seri No Takibi)
                    CREATE TABLE IF NOT EXISTS stock_items_serial (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        product_id UUID NOT NULL,
                        serial_number VARCHAR(100) UNIQUE NOT NULL,
                        lot_number VARCHAR(100),
                        expiry_date DATE,
                        gtin VARCHAR(50),
                        status VARCHAR(50) DEFAULT 'IN_STOCK' CHECK (status IN ('IN_STOCK', 'SOLD', 'WASTED', 'RETURNED')),
                        tracking_status VARCHAR(50) DEFAULT 'TRACKED' CHECK (tracking_status IN ('TRACKED', 'UNTRACKED')),
                        location_id UUID REFERENCES storage_locations(id),
                        acquired_cost DECIMAL(18,4),
                        acquired_date DATE,
                        sold_date TIMESTAMP WITH TIME ZONE,
                        sold_reference VARCHAR(100),
                        sold_by VARCHAR(100),
                        created_by VARCHAR(100),
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                    );
                    CREATE INDEX idx_stock_items_product ON stock_items_serial(product_id);
                    CREATE INDEX idx_stock_items_status ON stock_items_serial(status);
                    CREATE INDEX idx_stock_items_serial ON stock_items_serial(serial_number);
                    CREATE INDEX idx_stock_items_expiry ON stock_items_serial(expiry_date) WHERE expiry_date IS NOT NULL;
                    CREATE INDEX idx_stock_items_location ON stock_items_serial(location_id) WHERE location_id IS NOT NULL;

                    -- Batch Stok (OTC Parti Takibi)
                    CREATE TABLE IF NOT EXISTS stock_batches (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        product_id UUID NOT NULL,
                        batch_number VARCHAR(100),
                        expiry_date DATE NOT NULL,
                        quantity INT NOT NULL DEFAULT 0,
                        initial_quantity INT NOT NULL,
                        unit_cost DECIMAL(18,4) NOT NULL,
                        total_cost DECIMAL(18,2),
                        location_id UUID REFERENCES storage_locations(id),
                        is_active BOOLEAN DEFAULT TRUE,
                        created_by VARCHAR(100) NOT NULL,
                        created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
                        notes TEXT
                    );
                    CREATE INDEX idx_stock_batches_product ON stock_batches(product_id);
                    CREATE INDEX idx_stock_batches_expiry ON stock_batches(expiry_date);
                    CREATE INDEX idx_stock_batches_active ON stock_batches(is_active) WHERE is_active = TRUE;

                    -- Stok Özeti (Hızlı Sorgu İçin Cache)
                    CREATE TABLE IF NOT EXISTS stock_summary (
                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        product_id UUID UNIQUE NOT NULL,
                        total_tracked INT DEFAULT 0,
                        total_untracked INT DEFAULT 0,
                        total_quantity INT DEFAULT 0,
                        total_value DECIMAL(18,2) DEFAULT 0,
                        average_cost DECIMAL(18,4),
                        last_movement_date TIMESTAMP WITH TIME ZONE,
                        last_counted_date TIMESTAMP WITH TIME ZONE,
                        has_expiring_soon BOOLEAN DEFAULT FALSE,
                        has_expired BOOLEAN DEFAULT FALSE,
                        has_low_stock BOOLEAN DEFAULT FALSE,
                        needs_attention BOOLEAN DEFAULT FALSE,
                        updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                    );
                    CREATE INDEX idx_stock_summary_product ON stock_summary(product_id);
                    CREATE INDEX idx_stock_summary_expiring ON stock_summary(has_expiring_soon) WHERE has_expiring_soon = TRUE;
                    CREATE INDEX idx_stock_summary_low_stock ON stock_summary(has_low_stock) WHERE has_low_stock = TRUE;
                    CREATE INDEX idx_stock_summary_attention ON stock_summary(needs_attention) WHERE needs_attention = TRUE;

                    -- Otomatik Numara Üretme Fonksiyonu (Hareket Numarası İçin)
                    CREATE OR REPLACE FUNCTION generate_stock_movement_number()
                    RETURNS TEXT AS $func$
                    DECLARE
                        next_number INT;
                        year_str TEXT;
                        number_str TEXT;
                    BEGIN
                        year_str := TO_CHAR(NOW(), 'YYYY');
                        SELECT COALESCE(MAX(
                            CAST(SUBSTRING(movement_number FROM 'STK-' || year_str || '-(\d+)') AS INT)
                        ), 0) + 1 INTO next_number
                        FROM stock_movements
                        WHERE movement_number LIKE 'STK-' || year_str || '-%';
                        number_str := LPAD(next_number::TEXT, 5, '0');
                        RETURN 'STK-' || year_str || '-' || number_str;
                    END;
                    $func$ LANGUAGE plpgsql;

                    -- Batch Numara Üretme Fonksiyonu
                    CREATE OR REPLACE FUNCTION generate_batch_number()
                    RETURNS TEXT AS $func$
                    DECLARE
                        next_number INT;
                        year_str TEXT;
                        number_str TEXT;
                    BEGIN
                        year_str := TO_CHAR(NOW(), 'YYYY');
                        SELECT COALESCE(MAX(
                            CAST(SUBSTRING(batch_number FROM 'BATCH-' || year_str || '-(\d+)') AS INT)
                        ), 0) + 1 INTO next_number
                        FROM stock_batches
                        WHERE batch_number LIKE 'BATCH-' || year_str || '-%';
                        number_str := LPAD(next_number::TEXT, 3, '0');
                        RETURN 'BATCH-' || year_str || '-' || number_str;
                    END;
                    $func$ LANGUAGE plpgsql;

                    -- ========================================
                    -- SALES MODULE TABLES
                    -- ========================================
                    
                    -- Kesinleşmiş Satışlar
                    CREATE TABLE IF NOT EXISTS sales (
                        sale_id VARCHAR(50) PRIMARY KEY,
                        sale_number VARCHAR(50) UNIQUE NOT NULL,
                        sale_date TIMESTAMP NOT NULL DEFAULT NOW(),
                        
                        subtotal_amount DECIMAL(10,2) NOT NULL,
                        discount_amount DECIMAL(10,2) DEFAULT 0,
                        total_amount DECIMAL(10,2) NOT NULL,
                        
                        payment_method VARCHAR(20) NOT NULL,
                        payment_status VARCHAR(20) DEFAULT 'COMPLETED',
                        sale_type VARCHAR(20) NOT NULL DEFAULT 'NORMAL',
                        
                        customer_id VARCHAR(50),
                        customer_name VARCHAR(255),
                        customer_tc VARCHAR(11),
                        customer_phone VARCHAR(20),
                        
                        notes TEXT,
                        
                        created_by VARCHAR(100) NOT NULL,
                        created_at TIMESTAMP DEFAULT NOW(),
                        updated_at TIMESTAMP DEFAULT NOW(),
                        
                        fiscal_receipt_number VARCHAR(100),
                        fiscal_sent_at TIMESTAMP,
                        fiscal_status VARCHAR(20) DEFAULT 'PENDING',
                        fiscal_error_message TEXT,
                        
                        is_deleted BOOLEAN DEFAULT FALSE,
                        deleted_at TIMESTAMP,
                        deleted_by VARCHAR(100)
                    );
                    CREATE INDEX idx_sales_date ON sales(sale_date DESC);
                    CREATE INDEX idx_sales_created_by ON sales(created_by);
                    CREATE INDEX idx_sales_payment_method ON sales(payment_method);
                    CREATE INDEX idx_sales_fiscal_status ON sales(fiscal_status);
                    
                    -- Satış Detayları
                    CREATE TABLE IF NOT EXISTS sale_items (
                        id SERIAL PRIMARY KEY,
                        sale_id VARCHAR(50) NOT NULL REFERENCES sales(sale_id) ON DELETE CASCADE,
                        
                        product_id VARCHAR(100) NOT NULL,
                        product_name VARCHAR(500) NOT NULL,
                        product_category VARCHAR(20),
                        
                        quantity INT NOT NULL CHECK (quantity > 0),
                        unit_price DECIMAL(10,2) NOT NULL,
                        unit_cost DECIMAL(10,2),
                        discount_rate DECIMAL(5,2) DEFAULT 0,
                        total_price DECIMAL(10,2) NOT NULL,
                        
                        serial_number VARCHAR(100),
                        expiry_date DATE,
                        lot_number VARCHAR(50),
                        gtin VARCHAR(14),
                        
                        stock_movement_id VARCHAR(50),
                        stock_deducted BOOLEAN DEFAULT FALSE,
                        
                        created_at TIMESTAMP DEFAULT NOW()
                    );
                    CREATE INDEX idx_sale_items_sale_id ON sale_items(sale_id);
                    CREATE INDEX idx_sale_items_product_id ON sale_items(product_id);
                    CREATE INDEX idx_sale_items_gtin ON sale_items(gtin) WHERE gtin IS NOT NULL;
                    
                    -- İade İşlemleri (İleride kullanılacak)
                    CREATE TABLE IF NOT EXISTS sale_returns (
                        return_id VARCHAR(50) PRIMARY KEY,
                        return_number VARCHAR(50) UNIQUE NOT NULL,
                        original_sale_id VARCHAR(50) NOT NULL REFERENCES sales(sale_id),
                        return_date TIMESTAMP DEFAULT NOW(),
                        
                        return_amount DECIMAL(10,2) NOT NULL,
                        refund_method VARCHAR(20) NOT NULL,
                        reason TEXT NOT NULL,
                        return_type VARCHAR(20) DEFAULT 'FULL',
                        
                        created_by VARCHAR(100) NOT NULL,
                        created_at TIMESTAMP DEFAULT NOW(),
                        
                        stock_returned BOOLEAN DEFAULT FALSE
                    );
                    CREATE INDEX idx_returns_sale_id ON sale_returns(original_sale_id);
                    CREATE INDEX idx_returns_date ON sale_returns(return_date DESC);
                    
                    -- İade Detayları
                    CREATE TABLE IF NOT EXISTS sale_return_items (
                        id SERIAL PRIMARY KEY,
                        return_id VARCHAR(50) NOT NULL REFERENCES sale_returns(return_id) ON DELETE CASCADE,
                        original_sale_item_id INT NOT NULL REFERENCES sale_items(id),
                        
                        quantity_returned INT NOT NULL,
                        unit_price DECIMAL(10,2) NOT NULL,
                        total_amount DECIMAL(10,2) NOT NULL,
                        
                        stock_movement_id VARCHAR(50),
                        created_at TIMESTAMP DEFAULT NOW()
                    );
                    
                    -- Satış Numarası Üretme Fonksiyonu
                    CREATE OR REPLACE FUNCTION generate_sale_number()
                    RETURNS VARCHAR(50) AS $func$
                    DECLARE
                        today VARCHAR(8);
                        count INT;
                        new_number VARCHAR(50);
                    BEGIN
                        today := TO_CHAR(NOW(), 'YYYYMMDD');
                        SELECT COUNT(*) INTO count
                        FROM sales
                        WHERE sale_number LIKE 'SL-' || today || '-%';
                        new_number := 'SL-' || today || '-' || LPAD((count + 1)::TEXT, 3, '0');
                        RETURN new_number;
                    END;
                    $func$ LANGUAGE plpgsql;
                    
                    -- İade Numarası Üretme Fonksiyonu
                    CREATE OR REPLACE FUNCTION generate_return_number()
                    RETURNS VARCHAR(50) AS $func$
                    DECLARE
                        today VARCHAR(8);
                        count INT;
                        new_number VARCHAR(50);
                    BEGIN
                        today := TO_CHAR(NOW(), 'YYYYMMDD');
                        SELECT COUNT(*) INTO count
                        FROM sale_returns
                        WHERE return_number LIKE 'RET-' || today || '-%';
                        new_number := 'RET-' || today || '-' || LPAD((count + 1)::TEXT, 3, '0');
                        RETURN new_number;
                    END;
                    $func$ LANGUAGE plpgsql;

                    -- ========================================
                    -- CUSTOMER (HASTA/MÜŞTERİ) MODÜLÜ
                    -- ========================================
                    
                    -- Müşteriler tablosu
                    CREATE TABLE IF NOT EXISTS customers (
                        id TEXT PRIMARY KEY,
                        global_patient_id TEXT UNIQUE NOT NULL,
                        customer_type TEXT NOT NULL CHECK (customer_type IN ('INDIVIDUAL', 'FOREIGN', 'INFANT')),
                        
                        tc_no TEXT,
                        passport_no TEXT,
                        
                        mother_tc TEXT,
                        father_tc TEXT,
                        
                        guardian_tc TEXT,
                        guardian_name TEXT,
                        guardian_phone TEXT,
                        guardian_relation TEXT,
                        
                        first_name TEXT NOT NULL,
                        last_name TEXT NOT NULL,
                        phone TEXT NOT NULL,
                        birth_date DATE,
                        birth_year INT,
                        age INT,
                        gender TEXT CHECK (gender IN ('M', 'F', 'OTHER')),
                        
                        city TEXT,
                        district TEXT,
                        neighborhood TEXT,
                        street TEXT,
                        building_no TEXT,
                        apartment_no TEXT,
                        
                        emergency_contact_name TEXT,
                        emergency_contact_phone TEXT,
                        emergency_contact_relation TEXT,
                        
                        notes TEXT,
                        kvkk_consent BOOLEAN DEFAULT FALSE,
                        kvkk_consent_date TIMESTAMP,
                        is_active BOOLEAN DEFAULT TRUE,
                        created_at TIMESTAMP DEFAULT NOW(),
                        updated_at TIMESTAMP DEFAULT NOW(),
                        created_by TEXT,
                        
                        CONSTRAINT chk_customer_identity CHECK (
                            (customer_type = 'INDIVIDUAL' AND tc_no IS NOT NULL) OR
                            (customer_type = 'FOREIGN' AND passport_no IS NOT NULL) OR
                            (customer_type = 'INFANT' AND (mother_tc IS NOT NULL OR father_tc IS NOT NULL))
                        )
                    );
                    
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_customers_global_id ON customers(global_patient_id);
                    CREATE INDEX IF NOT EXISTS idx_customers_tc ON customers(tc_no) WHERE tc_no IS NOT NULL;
                    CREATE INDEX IF NOT EXISTS idx_customers_passport ON customers(passport_no) WHERE passport_no IS NOT NULL;
                    CREATE INDEX IF NOT EXISTS idx_customers_phone ON customers(phone);
                    CREATE INDEX IF NOT EXISTS idx_customers_name ON customers(first_name, last_name);
                    CREATE INDEX IF NOT EXISTS idx_customers_parent ON customers(mother_tc, father_tc) WHERE customer_type = 'INFANT';
                    CREATE INDEX IF NOT EXISTS idx_customers_created_at ON customers(created_at DESC);
                    
                    -- Trigger: updated_at otomatik güncelleme
                    CREATE OR REPLACE FUNCTION update_customers_updated_at()
                    RETURNS TRIGGER AS $trigfunc$
                    BEGIN
                        NEW.updated_at = NOW();
                        RETURN NEW;
                    END;
                    $trigfunc$ LANGUAGE plpgsql;
                    
                    CREATE TRIGGER trg_customers_updated_at
                        BEFORE UPDATE ON customers
                        FOR EACH ROW
                        EXECUTE FUNCTION update_customers_updated_at();
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

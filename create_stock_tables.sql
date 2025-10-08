-- ========================================
-- STOK MODÜLÜ TABLOLARI
-- Tenant DB'sine eklenecek tablolar
-- ========================================

-- 1. HAREKET TİPLERİ (Referans Tablo)
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

-- ========================================

-- 2. LOKASYONLAR (Raf/Dolap Takibi)
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

-- ========================================

-- 3. STOK HAREKETLERİ (DEFTER - EN ÖNEMLİ!)
CREATE TABLE IF NOT EXISTS stock_movements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    movement_number VARCHAR(50) UNIQUE NOT NULL,
    movement_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    movement_type VARCHAR(50) NOT NULL REFERENCES movement_types(code),
    
    product_id UUID NOT NULL,
    
    -- Miktar değişimi (pozitif=giriş, negatif=çıkış)
    quantity_change INT NOT NULL,
    
    -- Maliyet bilgileri (CARİ MODÜLÜ İÇİN KRİTİK!)
    unit_cost DECIMAL(18,4),
    total_cost DECIMAL(18,2),
    
    -- Karekod bilgileri (ilaçlar için)
    serial_number VARCHAR(100),
    lot_number VARCHAR(100),
    expiry_date DATE,
    gtin VARCHAR(50),
    
    -- Batch bilgisi (OTC için)
    batch_id UUID,
    
    -- Referans bilgisi (hangi işlemden geldi?)
    reference_type VARCHAR(50),
    reference_id VARCHAR(100),
    
    -- Lokasyon
    location_id UUID REFERENCES storage_locations(id),
    
    -- Kullanıcı takibi
    created_by VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    -- Düzeltme bilgileri
    is_correction BOOLEAN DEFAULT FALSE,
    correction_reason TEXT,
    
    -- Notlar
    notes TEXT
);

-- Index'ler (performans için)
CREATE INDEX idx_stock_movements_product ON stock_movements(product_id);
CREATE INDEX idx_stock_movements_date ON stock_movements(movement_date DESC);
CREATE INDEX idx_stock_movements_type ON stock_movements(movement_type);
CREATE INDEX idx_stock_movements_serial ON stock_movements(serial_number) WHERE serial_number IS NOT NULL;
CREATE INDEX idx_stock_movements_created_by ON stock_movements(created_by);
CREATE INDEX idx_stock_movements_reference ON stock_movements(reference_type, reference_id);

-- ========================================

-- 4. İLAÇ STOK DETAYI (Seri No Takibi)
CREATE TABLE IF NOT EXISTS stock_items_serial (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL,
    
    -- Karekod bilgileri
    serial_number VARCHAR(100) UNIQUE NOT NULL,
    lot_number VARCHAR(100),
    expiry_date DATE,
    gtin VARCHAR(50),
    
    -- Durum
    status VARCHAR(50) DEFAULT 'IN_STOCK' CHECK (status IN ('IN_STOCK', 'SOLD', 'WASTED', 'RETURNED')),
    tracking_status VARCHAR(50) DEFAULT 'TRACKED' CHECK (tracking_status IN ('TRACKED', 'UNTRACKED')),
    
    -- Lokasyon
    location_id UUID REFERENCES storage_locations(id),
    
    -- Maliyet
    acquired_cost DECIMAL(18,4),
    acquired_date DATE,
    
    -- Satış bilgisi (satıldıysa)
    sold_date TIMESTAMP WITH TIME ZONE,
    sold_reference VARCHAR(100),
    sold_by VARCHAR(100),
    
    -- Oluşturma bilgisi
    created_by VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_stock_items_product ON stock_items_serial(product_id);
CREATE INDEX idx_stock_items_status ON stock_items_serial(status);
CREATE INDEX idx_stock_items_serial ON stock_items_serial(serial_number);
CREATE INDEX idx_stock_items_expiry ON stock_items_serial(expiry_date) WHERE expiry_date IS NOT NULL;
CREATE INDEX idx_stock_items_location ON stock_items_serial(location_id) WHERE location_id IS NOT NULL;

-- ========================================

-- 5. BATCH STOK (OTC Parti Takibi)
CREATE TABLE IF NOT EXISTS stock_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL,
    
    -- Batch bilgisi
    batch_number VARCHAR(100),
    expiry_date DATE NOT NULL,
    
    -- Miktar
    quantity INT NOT NULL DEFAULT 0,
    initial_quantity INT NOT NULL,
    
    -- Maliyet
    unit_cost DECIMAL(18,4) NOT NULL,
    total_cost DECIMAL(18,2),
    
    -- Lokasyon
    location_id UUID REFERENCES storage_locations(id),
    
    -- Durum
    is_active BOOLEAN DEFAULT TRUE,
    
    -- Kullanıcı
    created_by VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    notes TEXT
);

CREATE INDEX idx_stock_batches_product ON stock_batches(product_id);
CREATE INDEX idx_stock_batches_expiry ON stock_batches(expiry_date);
CREATE INDEX idx_stock_batches_active ON stock_batches(is_active) WHERE is_active = TRUE;

-- ========================================

-- 6. STOK ÖZETİ (Hızlı Sorgu İçin Cache)
CREATE TABLE IF NOT EXISTS stock_summary (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID UNIQUE NOT NULL,
    
    -- İlaçlar için
    total_tracked INT DEFAULT 0,
    total_untracked INT DEFAULT 0,
    
    -- OTC için
    total_quantity INT DEFAULT 0,
    
    -- Finansal (CARİ MODÜLÜ İÇİN!)
    total_value DECIMAL(18,2) DEFAULT 0,
    average_cost DECIMAL(18,4),
    
    -- Tarihler
    last_movement_date TIMESTAMP WITH TIME ZONE,
    last_counted_date TIMESTAMP WITH TIME ZONE,
    
    -- Uyarılar
    has_expiring_soon BOOLEAN DEFAULT FALSE,
    has_expired BOOLEAN DEFAULT FALSE,
    has_low_stock BOOLEAN DEFAULT FALSE,
    needs_attention BOOLEAN DEFAULT FALSE,
    
    -- Güncelleme
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_stock_summary_product ON stock_summary(product_id);
CREATE INDEX idx_stock_summary_expiring ON stock_summary(has_expiring_soon) WHERE has_expiring_soon = TRUE;
CREATE INDEX idx_stock_summary_low_stock ON stock_summary(has_low_stock) WHERE has_low_stock = TRUE;
CREATE INDEX idx_stock_summary_attention ON stock_summary(needs_attention) WHERE needs_attention = TRUE;

-- ========================================

-- 7. ÜRÜNLER TABLOSUNA YENİ KOLONLAR EKLE
-- (Mevcut products tablosuna ekleme yapıyoruz)

-- Kategori kolonu
ALTER TABLE products ADD COLUMN IF NOT EXISTS category VARCHAR(50) DEFAULT 'DRUG' CHECK (category IN ('DRUG', 'NON_DRUG'));

-- Karekod var mı?
ALTER TABLE products ADD COLUMN IF NOT EXISTS has_datamatrix BOOLEAN DEFAULT FALSE;

-- SKT takibi gerekli mi?
ALTER TABLE products ADD COLUMN IF NOT EXISTS requires_expiry_tracking BOOLEAN DEFAULT TRUE;

-- Kontrollü ilaç mı?
ALTER TABLE products ADD COLUMN IF NOT EXISTS is_controlled BOOLEAN DEFAULT FALSE;

-- Index'ler
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);
CREATE INDEX IF NOT EXISTS idx_products_datamatrix ON products(has_datamatrix) WHERE has_datamatrix = TRUE;
CREATE INDEX IF NOT EXISTS idx_products_controlled ON products(is_controlled) WHERE is_controlled = TRUE;

-- ========================================

-- 8. OTOMATIK NUMARA ÜRETME FONKSİYONU (Hareket Numarası İçin)

CREATE OR REPLACE FUNCTION generate_stock_movement_number()
RETURNS TEXT AS $$
DECLARE
    next_number INT;
    year_str TEXT;
    number_str TEXT;
BEGIN
    -- Yıl (2025)
    year_str := TO_CHAR(NOW(), 'YYYY');
    
    -- Bu yıl için son numara
    SELECT COALESCE(MAX(
        CAST(SUBSTRING(movement_number FROM 'STK-' || year_str || '-(\d+)') AS INT)
    ), 0) + 1 INTO next_number
    FROM stock_movements
    WHERE movement_number LIKE 'STK-' || year_str || '-%';
    
    -- 5 haneli numara: 00001
    number_str := LPAD(next_number::TEXT, 5, '0');
    
    RETURN 'STK-' || year_str || '-' || number_str;
END;
$$ LANGUAGE plpgsql;

-- ========================================

-- 9. BATCH NUMARA ÜRETME FONKSİYONU

CREATE OR REPLACE FUNCTION generate_batch_number()
RETURNS TEXT AS $$
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
$$ LANGUAGE plpgsql;

-- ========================================

COMMENT ON TABLE stock_movements IS 'Tüm stok hareketleri (defter mantığı, asla silinmez/güncellenmez)';
COMMENT ON TABLE stock_items_serial IS 'İlaç kutu takibi (seri no bazlı, karekodlu ürünler)';
COMMENT ON TABLE stock_batches IS 'OTC ürün parti takibi (SKT grupları)';
COMMENT ON TABLE stock_summary IS 'Stok özet bilgisi (performans için cache)';
COMMENT ON TABLE movement_types IS 'Stok hareket tipleri (referans tablo)';
COMMENT ON TABLE storage_locations IS 'Raf/lokasyon takibi';

-- ========================================
-- İŞLEM TAMAMLANDI ✅
-- ========================================


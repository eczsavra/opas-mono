-- ========================================
-- SALES MODULE - DATABASE SCHEMA
-- ========================================
-- Bu tablolar tenant-specific olacak (opas_tenant_<GLN>)
-- TenantProvisioningService'e eklenecek
-- ========================================

-- ========================================
-- SALES TABLE (Kesinleşmiş Satışlar)
-- ========================================
CREATE TABLE IF NOT EXISTS sales (
    sale_id VARCHAR(50) PRIMARY KEY,              -- SALE_20250108_001
    sale_number VARCHAR(50) UNIQUE NOT NULL,      -- Auto-generated: SL-20250108-001
    sale_date TIMESTAMP NOT NULL DEFAULT NOW(),
    
    -- Finansal Bilgiler
    subtotal_amount DECIMAL(10,2) NOT NULL,       -- Ara toplam (indirim öncesi)
    discount_amount DECIMAL(10,2) DEFAULT 0,      -- İndirim tutarı
    total_amount DECIMAL(10,2) NOT NULL,          -- Genel toplam (indirim sonrası)
    
    -- Ödeme Bilgileri
    payment_method VARCHAR(20) NOT NULL,          -- CASH, CARD, CREDIT, CONSIGNMENT, IBAN, QR
    payment_status VARCHAR(20) DEFAULT 'COMPLETED', -- COMPLETED, PENDING, FAILED
    
    -- Satış Tipi
    sale_type VARCHAR(20) NOT NULL DEFAULT 'NORMAL', -- NORMAL, CONSIGNMENT
    
    -- Müşteri Bilgileri (Opsiyonel - Cari modülü için hazırlık)
    customer_id VARCHAR(50),                      -- İleride cari hesap ID
    customer_name VARCHAR(255),                   -- Şimdilik sadece isim
    customer_tc VARCHAR(11),                      -- Reçeteli satış için
    customer_phone VARCHAR(20),
    
    -- Notlar
    notes TEXT,
    
    -- Kullanıcı Takibi
    created_by VARCHAR(100) NOT NULL,             -- Username
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    -- YN ÖKC Entegrasyonu (İleride)
    fiscal_receipt_number VARCHAR(100),           -- Fiş numarası
    fiscal_sent_at TIMESTAMP,                     -- GİB'e gönderilme zamanı
    fiscal_status VARCHAR(20) DEFAULT 'PENDING',  -- PENDING, SENT, FAILED
    fiscal_error_message TEXT,
    
    -- Soft Delete
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(100)
);

-- ========================================
-- SALE_ITEMS TABLE (Satış Detayları)
-- ========================================
CREATE TABLE IF NOT EXISTS sale_items (
    id SERIAL PRIMARY KEY,
    sale_id VARCHAR(50) NOT NULL REFERENCES sales(sale_id) ON DELETE CASCADE,
    
    -- Ürün Bilgileri (Snapshot - değişebilir)
    product_id VARCHAR(100) NOT NULL,
    product_name VARCHAR(500) NOT NULL,           -- Satış anındaki isim
    product_category VARCHAR(20),                 -- PHARMACEUTICAL, OTC
    
    -- Miktar ve Fiyat
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10,2) NOT NULL,            -- Satış anındaki fiyat
    unit_cost DECIMAL(10,2),                      -- Maliyet (kar hesabı için)
    discount_rate DECIMAL(5,2) DEFAULT 0,         -- Ürün bazlı indirim
    total_price DECIMAL(10,2) NOT NULL,           -- Satır toplamı
    
    -- İlaç Takip Bilgileri (Opsiyonel - Karekoddan gelebilir)
    serial_number VARCHAR(100),                   -- Seri numarası
    expiry_date DATE,                             -- Son kullanma tarihi
    lot_number VARCHAR(50),                       -- Lot/Batch numarası
    gtin VARCHAR(14),                             -- GTIN (karekoddan)
    
    -- Stok Entegrasyonu
    stock_movement_id VARCHAR(50),                -- İlişkili stok hareketi
    stock_deducted BOOLEAN DEFAULT FALSE,         -- Stok düşüldü mü?
    
    -- Metadata
    created_at TIMESTAMP DEFAULT NOW()
);

-- ========================================
-- SALE_RETURNS TABLE (İade İşlemleri)
-- ========================================
-- İleride kullanılacak, şimdilik hazırlık
CREATE TABLE IF NOT EXISTS sale_returns (
    return_id VARCHAR(50) PRIMARY KEY,
    return_number VARCHAR(50) UNIQUE NOT NULL,    -- Auto-generated: RET-20250108-001
    original_sale_id VARCHAR(50) NOT NULL REFERENCES sales(sale_id),
    return_date TIMESTAMP DEFAULT NOW(),
    
    -- İade Tutarları
    return_amount DECIMAL(10,2) NOT NULL,
    refund_method VARCHAR(20) NOT NULL,           -- CASH, CARD, CREDIT
    
    -- İade Nedeni
    reason TEXT NOT NULL,
    return_type VARCHAR(20) DEFAULT 'FULL',       -- FULL, PARTIAL
    
    -- Kullanıcı
    created_by VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW(),
    
    -- Stok İadesi
    stock_returned BOOLEAN DEFAULT FALSE
);

-- ========================================
-- SALE_RETURN_ITEMS TABLE (İade Detayları)
-- ========================================
CREATE TABLE IF NOT EXISTS sale_return_items (
    id SERIAL PRIMARY KEY,
    return_id VARCHAR(50) NOT NULL REFERENCES sale_returns(return_id) ON DELETE CASCADE,
    original_sale_item_id INT NOT NULL REFERENCES sale_items(id),
    
    -- İade Bilgileri
    quantity_returned INT NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    
    -- Stok İadesi
    stock_movement_id VARCHAR(50),
    
    created_at TIMESTAMP DEFAULT NOW()
);

-- ========================================
-- AUTO-INCREMENT FUNCTIONS
-- ========================================

-- Sale Number Generator
CREATE OR REPLACE FUNCTION generate_sale_number()
RETURNS VARCHAR(50) AS $$
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
$$ LANGUAGE plpgsql;

-- Return Number Generator
CREATE OR REPLACE FUNCTION generate_return_number()
RETURNS VARCHAR(50) AS $$
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
$$ LANGUAGE plpgsql;

-- ========================================
-- INDEXES (Performance)
-- ========================================

CREATE INDEX IF NOT EXISTS idx_sales_date ON sales(sale_date DESC);
CREATE INDEX IF NOT EXISTS idx_sales_created_by ON sales(created_by);
CREATE INDEX IF NOT EXISTS idx_sales_payment_method ON sales(payment_method);
CREATE INDEX IF NOT EXISTS idx_sales_fiscal_status ON sales(fiscal_status);
CREATE INDEX IF NOT EXISTS idx_sales_customer ON sales(customer_id) WHERE customer_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_sale_items_sale_id ON sale_items(sale_id);
CREATE INDEX IF NOT EXISTS idx_sale_items_product_id ON sale_items(product_id);
CREATE INDEX IF NOT EXISTS idx_sale_items_gtin ON sale_items(gtin) WHERE gtin IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_sale_items_expiry ON sale_items(expiry_date) WHERE expiry_date IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_returns_sale_id ON sale_returns(original_sale_id);
CREATE INDEX IF NOT EXISTS idx_returns_date ON sale_returns(return_date DESC);

-- ========================================
-- COMMENTS
-- ========================================

COMMENT ON TABLE sales IS 'Kesinleşmiş satış kayıtları - Stok düşümü gerçekleşmiş';
COMMENT ON TABLE sale_items IS 'Satış detayları - Her satırdaki ürün bilgileri';
COMMENT ON TABLE sale_returns IS 'İade işlemleri - İleride kullanılacak';
COMMENT ON COLUMN sales.fiscal_status IS 'YN ÖKC entegrasyonu için - şimdilik kullanılmıyor';
COMMENT ON COLUMN sale_items.serial_number IS 'Karekoddan gelen seri no - opsiyonel';
COMMENT ON COLUMN sale_items.unit_cost IS 'Kar hesabı için - stock_movements.unit_cost ile eşleşir';

-- ========================================
-- SAMPLE QUERY (Test için)
-- ========================================

/*
-- Bugünkü satışlar
SELECT 
    s.sale_number,
    s.total_amount,
    s.payment_method,
    COUNT(si.id) as item_count
FROM sales s
LEFT JOIN sale_items si ON s.sale_id = si.sale_id
WHERE s.sale_date::date = CURRENT_DATE
GROUP BY s.sale_id
ORDER BY s.created_at DESC;

-- En çok satan ürünler
SELECT 
    si.product_name,
    SUM(si.quantity) as total_sold,
    SUM(si.total_price) as total_revenue
FROM sale_items si
JOIN sales s ON si.sale_id = s.sale_id
WHERE s.sale_date >= NOW() - INTERVAL '30 days'
GROUP BY si.product_id, si.product_name
ORDER BY total_sold DESC
LIMIT 10;
*/


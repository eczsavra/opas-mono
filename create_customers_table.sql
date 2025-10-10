-- Customers (Hasta/Müşteri) Table - Tenant-specific
-- Her eczane kendi müşterilerini görür
-- Aynı hasta (global_patient_id) farklı eczanelerde farklı tenant-specific ID'lere sahip olabilir

CREATE TABLE IF NOT EXISTS customers (
    -- Tenant-specific ID (human-readable)
    -- Format: HAS-{initials}-{GLN7}-{sequence}
    -- Örnek: HAS-AY-1530144-0000024
    id TEXT PRIMARY KEY,
    
    -- Global Patient ID (cross-tenant tracking)
    -- Format:
    -- - TC varsa: HAS-{TC_NO} (örn: HAS-12345678901)
    -- - Yabancı: HAS-F-{PASSPORT} (örn: HAS-F-AB1234567)
    -- - Bebek: HAS-P-{PARENT_TC}-{YYYYMMDD} (örn: HAS-P-98765432109-20251009)
    global_patient_id TEXT UNIQUE NOT NULL,
    
    -- Müşteri Tipi
    customer_type TEXT NOT NULL CHECK (customer_type IN ('INDIVIDUAL', 'FOREIGN', 'INFANT')),
    
    -- Kimlik Bilgileri
    tc_no TEXT,
    passport_no TEXT,
    
    -- Ebeveyn Bilgileri (bebek için)
    mother_tc TEXT,
    father_tc TEXT,
    
    -- Vasi/Veli Bilgileri (18 yaş altı için - komşu, akraba, kurum vs. olabilir)
    guardian_tc TEXT,
    guardian_name TEXT,
    guardian_phone TEXT,
    guardian_relation TEXT,
    
    -- Kişisel Bilgiler
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    phone TEXT NOT NULL,
    birth_date DATE,
    birth_year INT,
    age INT,
    gender TEXT CHECK (gender IN ('M', 'F', 'OTHER')),
    
    -- Adres
    city TEXT,
    district TEXT,
    neighborhood TEXT,
    street TEXT,
    building_no TEXT,
    apartment_no TEXT,
    
    -- Acil Durum
    emergency_contact_name TEXT,
    emergency_contact_phone TEXT,
    emergency_contact_relation TEXT,
    
    -- Meta
    notes TEXT,
    kvkk_consent BOOLEAN DEFAULT FALSE,
    kvkk_consent_date TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by TEXT,
    
    -- Constraints
    CONSTRAINT chk_customer_identity CHECK (
        (customer_type = 'INDIVIDUAL' AND tc_no IS NOT NULL) OR
        (customer_type = 'FOREIGN' AND passport_no IS NOT NULL) OR
        (customer_type = 'INFANT' AND (mother_tc IS NOT NULL OR father_tc IS NOT NULL))
    )
);

-- Indexes
CREATE UNIQUE INDEX IF NOT EXISTS idx_customers_global_id ON customers(global_patient_id);
CREATE INDEX IF NOT EXISTS idx_customers_tc ON customers(tc_no) WHERE tc_no IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_customers_passport ON customers(passport_no) WHERE passport_no IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_customers_phone ON customers(phone);
CREATE INDEX IF NOT EXISTS idx_customers_name ON customers(first_name, last_name);
CREATE INDEX IF NOT EXISTS idx_customers_parent ON customers(mother_tc, father_tc) WHERE customer_type = 'INFANT';
CREATE INDEX IF NOT EXISTS idx_customers_created_at ON customers(created_at DESC);

-- Trigger: updated_at otomatik güncelleme
CREATE OR REPLACE FUNCTION update_customers_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_customers_updated_at
    BEFORE UPDATE ON customers
    FOR EACH ROW
    EXECUTE FUNCTION update_customers_updated_at();

-- Comments
COMMENT ON TABLE customers IS 'Tenant-specific hasta/müşteri tablosu. Aynı hasta farklı eczanelerde farklı ID''lere sahip olabilir.';
COMMENT ON COLUMN customers.id IS 'Tenant-specific ID (örn: HAS-AY-1530144-0000024)';
COMMENT ON COLUMN customers.global_patient_id IS 'Cross-tenant global ID (örn: HAS-12345678901)';
COMMENT ON COLUMN customers.customer_type IS 'INDIVIDUAL (normal), FOREIGN (yabancı), INFANT (bebek)';


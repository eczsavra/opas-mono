-- =====================================================
-- OPAS VERİTABANI YENİDEN YAPILANDIRMA SCRIPT
-- =====================================================
-- Bu script central_products ve gln_registry tablolarını
-- Control Plane DB'den Public DB'ye taşır

-- 1. GLN_REGISTRY TABLOSUNU PUBLIC DB'YE TAŞI
-- =====================================================

-- Önce gln_registry tablosunu Public DB'de oluştur
CREATE TABLE IF NOT EXISTS gln_registry (
    id SERIAL PRIMARY KEY,
    gln VARCHAR(13) NOT NULL,
    company_name VARCHAR(200),
    authorized VARCHAR(200),
    email VARCHAR(200),
    phone VARCHAR(50),
    city VARCHAR(100),
    town VARCHAR(100),
    address VARCHAR(500),
    active BOOLEAN,
    source VARCHAR(50),
    imported_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index'leri oluştur
CREATE UNIQUE INDEX IF NOT EXISTS ux_gln_registry_gln ON gln_registry(gln);
CREATE INDEX IF NOT EXISTS idx_gln_registry_city ON gln_registry(city);
CREATE INDEX IF NOT EXISTS idx_gln_registry_active ON gln_registry(active);

-- 2. CENTRAL_PRODUCTS TABLOSUNU PUBLIC DB'DE OLUŞTUR
-- =====================================================

CREATE TABLE IF NOT EXISTS central_products (
    id SERIAL PRIMARY KEY,
    gtin VARCHAR(50),
    drug_name VARCHAR(500),
    manufacturer_name VARCHAR(500),
    manufacturer_gln VARCHAR(13),
    is_active BOOLEAN DEFAULT true,
    is_imported BOOLEAN DEFAULT false,
    price DECIMAL(10,2),
    last_its_sync_at TIMESTAMPTZ,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index'leri oluştur
CREATE INDEX IF NOT EXISTS idx_central_products_gtin ON central_products(gtin);
CREATE INDEX IF NOT EXISTS idx_central_products_drug_name ON central_products(drug_name);
CREATE INDEX IF NOT EXISTS idx_central_products_manufacturer ON central_products(manufacturer_name);
CREATE INDEX IF NOT EXISTS idx_central_products_active ON central_products(is_active);

-- 3. PRODUCT_REF TABLOSUNU SİL (GEREKSIZ)
-- =====================================================

DROP TABLE IF EXISTS product_ref;

-- 4. VERİ TAŞIMA (EĞER GEREKİYORSA)
-- =====================================================
-- Not: Bu kısım manuel olarak yapılacak çünkü cross-database işlem gerekiyor

COMMENT ON TABLE gln_registry IS 'ITS stakeholder import: eczane GLN listesi';
COMMENT ON TABLE central_products IS 'Merkezi ürün listesi (ITS''den gelen)';

-- =====================================================
-- SCRIPT TAMAMLANDI
-- =====================================================

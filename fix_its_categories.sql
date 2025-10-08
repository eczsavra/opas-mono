-- ===========================================
-- ITS'DEN GELEN TÜM ÜRÜNLERİ İLAÇ OLARAK İŞARETLE
-- ===========================================
-- 
-- AÇIKLAMA:
-- ITS'den çekilen tüm ürünler ilaçtır (PHARMACEUTICAL).
-- Category alanını "DRUG" olarak güncelliyoruz.
-- 
-- KULLANIM:
-- 1. PostgreSQL client'ı aç (pgAdmin, DBeaver, psql vb.)
-- 2. Tenant veritabanına bağlan (örn: opas_tenant_8680001530144)
-- 3. Bu script'i çalıştır
-- 
-- ===========================================

-- Tenant DB'sine bağlan (örnek: opas_tenant_8680001530144)
-- \c opas_tenant_8680001530144

BEGIN;

-- 1. Mevcut durumu göster
SELECT 
    category,
    COUNT(*) as product_count
FROM products
GROUP BY category;

-- 2. ITS'den gelen ürünleri güncelle (last_its_sync_at NOT NULL olanlar)
UPDATE products
SET 
    category = 'DRUG',
    has_datamatrix = true,
    requires_expiry_tracking = true,
    updated_at_utc = NOW()
WHERE 
    last_its_sync_at IS NOT NULL  -- ITS'den gelen ürünler
    AND category != 'DRUG';        -- Sadece farklı olanları güncelle

-- 3. Güncelleme sonrası durumu göster
SELECT 
    category,
    COUNT(*) as product_count
FROM products
GROUP BY category;

-- 4. Örnek ürünleri kontrol et
SELECT 
    drug_name,
    category,
    has_datamatrix,
    last_its_sync_at
FROM products
WHERE drug_name ILIKE '%PAROL%' OR drug_name ILIKE '%ARVELES%'
LIMIT 5;

COMMIT;

-- Eğer değişikliklerden memnun değilseniz ROLLBACK; yazın



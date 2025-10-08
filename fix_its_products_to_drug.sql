-- ITS'den gelen tüm ürünleri ilaç olarak işaretle
-- Çünkü ITS'de sadece ilaçlar var, OTC/kozmetik yok

-- Control DB'deki central_products tablosunu güncelle
UPDATE central_products
SET 
    category = 'DRUG',
    has_datamatrix = TRUE,
    requires_expiry_tracking = TRUE,
    is_controlled = FALSE
WHERE category IS NULL OR category = 'NON_DRUG' OR has_datamatrix = FALSE;

-- Tenant DB'lerindeki products tablolarını da güncelle
-- Her tenant için ayrı ayrı çalıştırılmalı

-- Örnek: Tenant 8680001530144 için
-- Kendi GLN'inizi yazın!
UPDATE products
SET 
    category = 'DRUG',
    has_datamatrix = TRUE,
    requires_expiry_tracking = TRUE,
    is_controlled = FALSE
WHERE category IS NULL OR category = 'NON_DRUG' OR has_datamatrix = FALSE;

-- Kontrol için:
SELECT 
    COUNT(*) as toplam_urun,
    COUNT(CASE WHEN category = 'DRUG' THEN 1 END) as ilac_sayisi,
    COUNT(CASE WHEN has_datamatrix = TRUE THEN 1 END) as datamatrix_sayisi
FROM products;


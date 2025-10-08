-- Mal Fazlası (MF) kolonlarını stock_movements tablosuna ekle
-- Örnek: 10+1, 20+3 gibi kampanyalar

ALTER TABLE stock_movements 
ADD COLUMN IF NOT EXISTS bonus_quantity INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS bonus_ratio VARCHAR(20);

COMMENT ON COLUMN stock_movements.bonus_quantity IS 'Mal Fazlası hediye adet (örn: 10+1 için 1)';
COMMENT ON COLUMN stock_movements.bonus_ratio IS 'Görsel gösterim (örn: "10+1", "20+3")';

-- Index ekle (mal fazlası olan hareketleri hızlı bulmak için)
CREATE INDEX IF NOT EXISTS idx_stock_movements_bonus ON stock_movements(bonus_quantity) WHERE bonus_quantity > 0;


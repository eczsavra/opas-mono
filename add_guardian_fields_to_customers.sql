-- Migration: Add guardian_tc and guardian_relation fields to customers table
-- To run: Execute this SQL on EACH tenant database (opas_tenant_XXXXXXX)

-- Add new columns
ALTER TABLE customers 
    ADD COLUMN IF NOT EXISTS guardian_tc TEXT,
    ADD COLUMN IF NOT EXISTS guardian_relation TEXT;

-- Add index for guardian_tc for faster lookups
CREATE INDEX IF NOT EXISTS idx_customers_guardian_tc ON customers(guardian_tc) WHERE guardian_tc IS NOT NULL;

-- Update comments
COMMENT ON COLUMN customers.guardian_tc IS 'Vasi/Veli TC kimlik numarası (komşu, akraba, kurum vs. olabilir)';
COMMENT ON COLUMN customers.guardian_relation IS 'Vasi ile ilişki (Anne, Baba, Amca, Dayı, Komşu, vs.)';

-- Verify
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'customers' 
AND column_name IN ('guardian_tc', 'guardian_name', 'guardian_phone', 'guardian_relation')
ORDER BY ordinal_position;


-- Align gln_registry table to ITS stakeholder response
-- Adds/renames columns and comments for clarity

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'gln_registry') THEN
        CREATE TABLE gln_registry (
            id serial PRIMARY KEY,
            gln varchar(13) NOT NULL,
            company_name varchar(200) NULL,
            authorized varchar(200) NULL,
            email varchar(200) NULL,
            phone varchar(50) NULL,
            city varchar(100) NULL,
            town varchar(100) NULL,
            address varchar(500) NULL,
            active boolean NULL,
            imported_at_utc timestamptz NOT NULL DEFAULT now()
        );
        CREATE UNIQUE INDEX ux_gln_registry_gln ON gln_registry(gln);
    END IF;
END $$;

-- Ensure columns exist with correct types
ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS company_name varchar(200);
COMMENT ON COLUMN gln_registry.company_name IS 'ITS: companyName (eczane adı)';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS authorized varchar(200);
COMMENT ON COLUMN gln_registry.authorized IS 'ITS: authorized (yetkili kişi)';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS email varchar(200);
COMMENT ON COLUMN gln_registry.email IS 'ITS: email';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS phone varchar(50);
COMMENT ON COLUMN gln_registry.phone IS 'ITS: phone';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS city varchar(100);
COMMENT ON COLUMN gln_registry.city IS 'ITS: city (il)';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS town varchar(100);
COMMENT ON COLUMN gln_registry.town IS 'ITS: town (ilçe)';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS address varchar(500);
COMMENT ON COLUMN gln_registry.address IS 'ITS: address';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS active boolean;
COMMENT ON COLUMN gln_registry.active IS 'ITS: active (true/false)';

ALTER TABLE gln_registry ADD COLUMN IF NOT EXISTS imported_at_utc timestamptz;
COMMENT ON COLUMN gln_registry.imported_at_utc IS 'Import timestamp (UTC)';

-- Base columns and constraints
ALTER TABLE gln_registry ALTER COLUMN gln TYPE varchar(13);
COMMENT ON COLUMN gln_registry.gln IS 'ITS: gln (13 hane)';

COMMENT ON TABLE gln_registry IS 'ITS stakeholder import: eczane GLN listesi';



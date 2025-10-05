-- Create log_entries table in opas_control database
-- Usage: psql -U postgres -h 127.0.0.1 -d opas_control -f create_log_entries_table.sql

CREATE TABLE IF NOT EXISTS log_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    level VARCHAR(50) NOT NULL,
    message VARCHAR(2000) NOT NULL,
    user_id VARCHAR(50),
    tenant_id VARCHAR(50),
    correlation_id VARCHAR(100),
    client_ip VARCHAR(45),
    user_agent VARCHAR(500),
    request_path VARCHAR(500),
    request_method VARCHAR(10),
    status_code INTEGER,
    duration_ms BIGINT,
    exception VARCHAR(4000),
    properties VARCHAR(4000),
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMP WITH TIME ZONE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_log_entries_correlation_id ON log_entries(correlation_id);
CREATE INDEX IF NOT EXISTS idx_log_entries_level ON log_entries(level);
CREATE INDEX IF NOT EXISTS idx_log_entries_tenant_id ON log_entries(tenant_id);
CREATE INDEX IF NOT EXISTS idx_log_entries_timestamp ON log_entries(timestamp);
CREATE INDEX IF NOT EXISTS idx_log_entries_user_id ON log_entries(user_id);

-- Verify
SELECT 'log_entries table created successfully!' AS result;


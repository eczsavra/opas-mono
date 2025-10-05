-- Add UNIQUE constraint to draft_sales.tab_id
-- This is required for ON CONFLICT (tab_id) to work in upsert queries
-- Usage: Run this for EACH tenant database

-- Add UNIQUE constraint
ALTER TABLE draft_sales 
ADD CONSTRAINT uq_draft_sales_tab_id UNIQUE (tab_id);

-- Verify
SELECT 'UNIQUE constraint added successfully!' AS result;


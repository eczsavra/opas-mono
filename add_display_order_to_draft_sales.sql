-- Add display_order column to draft_sales table for maintaining tab order

DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'draft_sales' 
        AND column_name = 'display_order'
    ) THEN
        ALTER TABLE draft_sales 
        ADD COLUMN display_order INT DEFAULT 0;
        
        RAISE NOTICE 'Column display_order added to draft_sales table';
        
        -- Set initial order based on created_at
        UPDATE draft_sales 
        SET display_order = sub.row_num
        FROM (
            SELECT id, ROW_NUMBER() OVER (ORDER BY created_at ASC) as row_num
            FROM draft_sales
        ) sub
        WHERE draft_sales.id = sub.id;
        
        RAISE NOTICE 'Initial display_order values set';
    ELSE
        RAISE NOTICE 'Column display_order already exists in draft_sales table';
    END IF;
END $$;

-- Create index for faster ordering
CREATE INDEX IF NOT EXISTS idx_draft_sales_display_order 
ON draft_sales(display_order) 
WHERE is_completed = FALSE;

SELECT 'Migration complete!' AS status;


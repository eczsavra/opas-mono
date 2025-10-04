-- Fix migration history for opas_control database
-- Mark existing migrations as applied

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('20250924014103_AddPharmacistAdminTables', '8.0.0'),
    ('20250925013641_AddLogsTable', '8.0.0'),
    ('20250925014016_AddLogEntriesTable', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;


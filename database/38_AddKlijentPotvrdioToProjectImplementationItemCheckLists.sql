-- Add KlijentPotvrdio and KlijentPotvrdioDatum to ProjectImplementationItemCheckLists
-- EF Migration: 20260401175236_AddKlijentPotvrdioToCheckListItems

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectImplementationItemCheckLists]') AND name = 'KlijentPotvrdio')
BEGIN
    ALTER TABLE [ProjectImplementationItemCheckLists] ADD [KlijentPotvrdio] bit NOT NULL DEFAULT CAST(0 AS bit);
    PRINT 'Column KlijentPotvrdio added.';
END
ELSE
    PRINT 'Column KlijentPotvrdio already exists.';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectImplementationItemCheckLists]') AND name = 'KlijentPotvrdioDatum')
BEGIN
    ALTER TABLE [ProjectImplementationItemCheckLists] ADD [KlijentPotvrdioDatum] datetime2 NULL;
    PRINT 'Column KlijentPotvrdioDatum added.';
END
ELSE
    PRINT 'Column KlijentPotvrdioDatum already exists.';

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260401175236_AddKlijentPotvrdioToCheckListItems')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260401175236_AddKlijentPotvrdioToCheckListItems', N'8.0.0');
    PRINT 'Migration history updated.';
END

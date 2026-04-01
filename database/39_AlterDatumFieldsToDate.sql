-- Alter datum columns from datetime/datetime2 to date in ProjectImplementationItems and ProjectImplementationItemCheckLists
-- EF Migration: 20260401181245_AlterDatumFieldsToDateOnly

-- ProjectImplementationItems.ZavrsenoDatum
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectImplementationItems]') AND name = 'ZavrsenoDatum' AND system_type_id IN (TYPE_ID(N'datetime2'), TYPE_ID(N'datetime')))
BEGIN
    ALTER TABLE [ProjectImplementationItems] ALTER COLUMN [ZavrsenoDatum] date NULL;
    PRINT 'ProjectImplementationItems.ZavrsenoDatum changed to date.';
END
ELSE
    PRINT 'ProjectImplementationItems.ZavrsenoDatum already date or does not exist.';

-- ProjectImplementationItems.KlijentPotvrdioDatum
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectImplementationItems]') AND name = 'KlijentPotvrdioDatum' AND system_type_id IN (TYPE_ID(N'datetime2'), TYPE_ID(N'datetime')))
BEGIN
    ALTER TABLE [ProjectImplementationItems] ALTER COLUMN [KlijentPotvrdioDatum] date NULL;
    PRINT 'ProjectImplementationItems.KlijentPotvrdioDatum changed to date.';
END
ELSE
    PRINT 'ProjectImplementationItems.KlijentPotvrdioDatum already date or does not exist.';

-- ProjectImplementationItemCheckLists.ZavrsenDatum
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectImplementationItemCheckLists]') AND name = 'ZavrsenDatum' AND system_type_id IN (TYPE_ID(N'datetime2'), TYPE_ID(N'datetime')))
BEGIN
    ALTER TABLE [ProjectImplementationItemCheckLists] ALTER COLUMN [ZavrsenDatum] date NULL;
    PRINT 'ProjectImplementationItemCheckLists.ZavrsenDatum changed to date.';
END
ELSE
    PRINT 'ProjectImplementationItemCheckLists.ZavrsenDatum already date or does not exist.';

-- ProjectImplementationItemCheckLists.KlijentPotvrdioDatum
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectImplementationItemCheckLists]') AND name = 'KlijentPotvrdioDatum' AND system_type_id IN (TYPE_ID(N'datetime2'), TYPE_ID(N'datetime')))
BEGIN
    ALTER TABLE [ProjectImplementationItemCheckLists] ALTER COLUMN [KlijentPotvrdioDatum] date NULL;
    PRINT 'ProjectImplementationItemCheckLists.KlijentPotvrdioDatum changed to date.';
END
ELSE
    PRINT 'ProjectImplementationItemCheckLists.KlijentPotvrdioDatum already date or does not exist.';

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260401181245_AlterDatumFieldsToDateOnly')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260401181245_AlterDatumFieldsToDateOnly', N'8.0.0');
    PRINT 'Migration history updated.';
END

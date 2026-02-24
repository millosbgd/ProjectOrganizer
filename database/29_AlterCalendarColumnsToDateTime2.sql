-- =====================================================
-- Convert calendar columns from DATETIME to DATETIME2
-- to fix timezone/precision issues with UTC timestamps
-- =====================================================
-- This migration:
-- 1. Backs up existing data
-- 2. Converts StartUtc and EndUtc to DATETIME2
-- 3. Recreates indexes
-- 4. Provides rollback script if needed
-- =====================================================

USE ProjectOrganizer;
GO

SET NOCOUNT ON;
PRINT '========================================';
PRINT 'Starting UTC DateTime2 Migration';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- Step 1: Show current column types
PRINT '-- Current column information:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Aktivnosti')
    AND c.name IN ('StartUtc', 'EndUtc');
PRINT '';

-- Step 2: Count existing records with time data
DECLARE @RecordCount INT;
SELECT @RecordCount = COUNT(*) FROM Aktivnosti WHERE StartUtc IS NOT NULL OR EndUtc IS NOT NULL;
PRINT '-- Records with time data: ' + CAST(@RecordCount AS VARCHAR(10));
PRINT '';

-- Step 3: Create backup table (optional, uncomment if needed)
/*
IF OBJECT_ID('Aktivnosti_TimeBackup', 'U') IS NOT NULL
    DROP TABLE Aktivnosti_TimeBackup;

SELECT Id, StartUtc, EndUtc, Datum, Opis
INTO Aktivnosti_TimeBackup
FROM Aktivnosti
WHERE StartUtc IS NOT NULL OR EndUtc IS NOT NULL;

PRINT '-- Backup created: Aktivnosti_TimeBackup (' + CAST(@RecordCount AS VARCHAR(10)) + ' records)';
PRINT '';
*/

-- Step 4: Check and alter StartUtc column
BEGIN TRY
    IF EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID('Aktivnosti') 
        AND name = 'StartUtc'
        AND system_type_id = TYPE_ID('datetime') -- Check if it's currently datetime
    )
    BEGIN
        PRINT '-- Processing StartUtc column...';
        
        -- Drop dependent indexes
        IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'IX_Aktivnosti_StartUtc')
        BEGIN
            DROP INDEX IX_Aktivnosti_StartUtc ON Aktivnosti;
            PRINT '  ✓ Dropped index: IX_Aktivnosti_StartUtc';
        END
        
        IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'IX_Aktivnosti_TimeRange')
        BEGIN
            DROP INDEX IX_Aktivnosti_TimeRange ON Aktivnosti;
            PRINT '  ✓ Dropped index: IX_Aktivnosti_TimeRange';
        END
        
        IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'IX_Aktivnosti_CreatedBy_StartUtc_EndUtc')
        BEGIN
            DROP INDEX IX_Aktivnosti_CreatedBy_StartUtc_EndUtc ON Aktivnosti;
            PRINT '  ✓ Dropped index: IX_Aktivnosti_CreatedBy_StartUtc_EndUtc';
        END
        
        -- Alter column to datetime2
        ALTER TABLE Aktivnosti 
        ALTER COLUMN StartUtc DATETIME2(7) NULL;
        
        PRINT '  ✓ Column altered: StartUtc -> DATETIME2(7)';
        
        -- Recreate index
        CREATE NONCLUSTERED INDEX IX_Aktivnosti_StartUtc ON Aktivnosti(StartUtc);
        PRINT '  ✓ Index recreated: IX_Aktivnosti_StartUtc';
        PRINT '';
    END
    ELSE
    BEGIN
        PRINT '-- StartUtc column already DATETIME2 or does not exist';
        PRINT '';
    END
END TRY
BEGIN CATCH
    PRINT 'ERROR altering StartUtc: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

-- Step 5: Check and alter EndUtc column
BEGIN TRY
    IF EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID('Aktivnosti') 
        AND name = 'EndUtc'
        AND system_type_id = TYPE_ID('datetime') -- Check if it's currently datetime
    )
    BEGIN
        PRINT '-- Processing EndUtc column...';
        
        -- Drop dependent index
        IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'IX_Aktivnosti_EndUtc')
        BEGIN
            DROP INDEX IX_Aktivnosti_EndUtc ON Aktivnosti;
            PRINT '  ✓ Dropped index: IX_Aktivnosti_EndUtc';
        END
        
        -- Alter column to datetime2
        ALTER TABLE Aktivnosti 
        ALTER COLUMN EndUtc DATETIME2(7) NULL;
        
        PRINT '  ✓ Column altered: EndUtc -> DATETIME2(7)';
        
        -- Recreate index
        CREATE NONCLUSTERED INDEX IX_Aktivnosti_EndUtc ON Aktivnosti(EndUtc);
        PRINT '  ✓ Index recreated: IX_Aktivnosti_EndUtc';
        PRINT '';
    END
    ELSE
    BEGIN
        PRINT '-- EndUtc column already DATETIME2 or does not exist';
        PRINT '';
    END
END TRY
BEGIN CATCH
    PRINT 'ERROR altering EndUtc: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO

-- Step 6: Recreate composite indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'IX_Aktivnosti_TimeRange')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Aktivnosti_TimeRange 
    ON Aktivnosti(StartUtc, EndUtc);
    PRINT '-- ✓ Created composite index: IX_Aktivnosti_TimeRange';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'IX_Aktivnosti_CreatedBy_StartUtc_EndUtc')
    AND EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'CreatedBy')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Aktivnosti_CreatedBy_StartUtc_EndUtc 
    ON Aktivnosti(CreatedBy, StartUtc, EndUtc)
    WHERE StartUtc IS NOT NULL AND EndUtc IS NOT NULL;
    PRINT '-- ✓ Created index: IX_Aktivnosti_CreatedBy_StartUtc_EndUtc';
END
GO

-- Step 7: Verify migration
PRINT '';
PRINT '-- Final column information:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS Precision,
    c.scale AS Scale,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Aktivnosti')
    AND c.name IN ('StartUtc', 'EndUtc');

PRINT '';
PRINT '-- Indexes on Aktivnosti table:';
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
    ic.key_ordinal AS KeyOrdinal
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('Aktivnosti')
    AND i.name LIKE '%Utc%'
ORDER BY i.name, ic.key_ordinal;

PRINT '';
PRINT '========================================';
PRINT 'Migration completed successfully!';
PRINT '========================================';
PRINT '';

/*
-- ROLLBACK SCRIPT (if needed):
-- =====================================================
-- Run this only if you need to revert the changes
-- =====================================================

USE ProjectOrganizer;
GO

-- Drop indexes
DROP INDEX IF EXISTS IX_Aktivnosti_StartUtc ON Aktivnosti;
DROP INDEX IF EXISTS IX_Aktivnosti_EndUtc ON Aktivnosti;
DROP INDEX IF EXISTS IX_Aktivnosti_TimeRange ON Aktivnosti;
DROP INDEX IF EXISTS IX_Aktivnosti_CreatedBy_StartUtc_EndUtc ON Aktivnosti;

-- Revert to DATETIME
ALTER TABLE Aktivnosti ALTER COLUMN StartUtc DATETIME NULL;
ALTER TABLE Aktivnosti ALTER COLUMN EndUtc DATETIME NULL;

-- Recreate indexes
CREATE INDEX IX_Aktivnosti_StartUtc ON Aktivnosti(StartUtc);
CREATE INDEX IX_Aktivnosti_EndUtc ON Aktivnosti(EndUtc);
CREATE INDEX IX_Aktivnosti_TimeRange ON Aktivnosti(StartUtc, EndUtc);

-- Restore from backup (if created)
-- UPDATE a SET 
--     a.StartUtc = b.StartUtc,
--     a.EndUtc = b.EndUtc
-- FROM Aktivnosti a
-- INNER JOIN Aktivnosti_TimeBackup b ON a.Id = b.Id;

PRINT 'Rollback completed';
*/
GO

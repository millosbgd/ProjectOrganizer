-- Add StartUtc and EndUtc columns to Aktivnosti table
-- This enables calendar functionality with time ranges

USE ProjectOrganizer;
GO

-- Add new columns (nullable first to allow existing data)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'StartUtc')
BEGIN
    ALTER TABLE Aktivnosti
    ADD StartUtc DATETIME NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Aktivnosti') AND name = 'EndUtc')
BEGIN
    ALTER TABLE Aktivnosti
    ADD EndUtc DATETIME NULL;
END
GO

-- Migrate existing data: use Datum as start time (9:00 AM UTC) and add 1 hour duration
UPDATE Aktivnosti
SET 
    StartUtc = CAST(CAST(Datum AS DATE) AS DATETIME) + CAST('09:00:00' AS DATETIME),
    EndUtc = CAST(CAST(Datum AS DATE) AS DATETIME) + CAST('10:00:00' AS DATETIME)
WHERE StartUtc IS NULL OR EndUtc IS NULL;
GO

-- Add indexes for performance (as per specification)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Aktivnosti_StartUtc')
BEGIN
    CREATE INDEX IX_Aktivnosti_StartUtc ON Aktivnosti(StartUtc);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Aktivnosti_EndUtc')
BEGIN
    CREATE INDEX IX_Aktivnosti_EndUtc ON Aktivnosti(EndUtc);
END
GO

-- Composite index for range queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Aktivnosti_TimeRange')
BEGIN
    CREATE INDEX IX_Aktivnosti_TimeRange ON Aktivnosti(StartUtc, EndUtc);
END
GO

PRINT 'Calendar columns and indexes added successfully to Aktivnosti table.';
PRINT 'Existing activities migrated with default 1-hour duration starting at 9:00 AM UTC.';
GO

-- Convert Datum column from datetime2 to date to fix timezone issues
-- DateOnly type in .NET maps to 'date' type in SQL Server

-- Check if column exists and alter it
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('Projekti') 
    AND name = 'Datum'
)
BEGIN
    PRINT 'Altering Projekti.Datum column from datetime2 to date'
    
    -- ALTER column to date type
    ALTER TABLE Projekti 
    ALTER COLUMN Datum date NOT NULL;
    
    PRINT 'Column altered successfully'
END
ELSE
BEGIN
    PRINT 'Column Projekti.Datum does not exist'
END
GO

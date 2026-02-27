-- Add PDV (VAT) related fields to Klijenti table
-- PdvStatus - PDV registration status from NBS API
-- PdvRegistrationDate - Date when company was registered for PDV

USE ProjectOrganizer;
GO

-- Check if columns don't exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Klijenti]') AND name = 'PdvStatus')
BEGIN
    ALTER TABLE Klijenti
    ADD PdvStatus NVARCHAR(100) NULL;
    
    PRINT 'Column PdvStatus added to Klijenti table';
END
ELSE
BEGIN
    PRINT 'Column PdvStatus already exists in Klijenti table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Klijenti]') AND name = 'PdvRegistrationDate')
BEGIN
    ALTER TABLE Klijenti
    ADD PdvRegistrationDate NVARCHAR(50) NULL;
    
    PRINT 'Column PdvRegistrationDate added to Klijenti table';
END
ELSE
BEGIN
    PRINT 'Column PdvRegistrationDate already exists in Klijenti table';
END
GO

-- Add index on PdvStatus for faster filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Klijenti_PdvStatus' AND object_id = OBJECT_ID('Klijenti'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Klijenti_PdvStatus ON Klijenti(PdvStatus);
    PRINT 'Index IX_Klijenti_PdvStatus created';
END
ELSE
BEGIN
    PRINT 'Index IX_Klijenti_PdvStatus already exists';
END
GO

PRINT 'Migration 30_AddPdvFieldsToKlijenti.sql completed successfully';
GO

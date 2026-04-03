-- Migration: Add InternalGoogleSheetId to Projekti
-- Purpose: Store the ID of the internal Google Sheet (separate from client-facing GoogleSheetId)

ALTER TABLE Projekti
ADD InternalGoogleSheetId NVARCHAR(100) NULL;
GO

PRINT 'AddInternalGoogleSheetId migration applied.';

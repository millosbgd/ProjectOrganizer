-- Add Kompleksnost column to CheckListItems table
-- Kompleksnost is a metric field with values from 1 to 10 (decimal)

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CheckListItems') AND name = 'Kompleksnost')
BEGIN
    ALTER TABLE [dbo].[CheckListItems]
    ADD [Kompleksnost] DECIMAL(3,1) NULL;
    
    PRINT 'Kompleksnost column added to CheckListItems table.';
END
ELSE
BEGIN
    PRINT 'Kompleksnost column already exists in CheckListItems table.';
END
GO

-- Add constraint to ensure Kompleksnost is between 1 and 10
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_CheckListItems_Kompleksnost')
BEGIN
    ALTER TABLE [dbo].[CheckListItems]
    ADD CONSTRAINT [CK_CheckListItems_Kompleksnost] 
    CHECK ([Kompleksnost] IS NULL OR ([Kompleksnost] >= 1.0 AND [Kompleksnost] <= 10.0));
    
    PRINT 'Constraint CK_CheckListItems_Kompleksnost created.';
END
ELSE
BEGIN
    PRINT 'Constraint CK_CheckListItems_Kompleksnost already exists.';
END
GO

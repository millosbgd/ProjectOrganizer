-- Add Procenat column to ProjectImplementationItemCheckLists table
-- Procenat represents the percentage weight based on CheckListItem Kompleksnost

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProjectImplementationItemCheckLists') AND name = 'Procenat')
BEGIN
    ALTER TABLE [dbo].[ProjectImplementationItemCheckLists]
    ADD [Procenat] DECIMAL(5,2) NULL;
    
    PRINT 'Procenat column added to ProjectImplementationItemCheckLists table.';
END
ELSE
BEGIN
    PRINT 'Procenat column already exists in ProjectImplementationItemCheckLists table.';
END
GO

-- Add constraint to ensure Procenat is between 0 and 100
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_ProjectImplementationItemCheckLists_Procenat')
BEGIN
    ALTER TABLE [dbo].[ProjectImplementationItemCheckLists]
    ADD CONSTRAINT [CK_ProjectImplementationItemCheckLists_Procenat] 
    CHECK ([Procenat] IS NULL OR ([Procenat] >= 0.0 AND [Procenat] <= 100.0));
    
    PRINT 'Constraint CK_ProjectImplementationItemCheckLists_Procenat created.';
END
ELSE
BEGIN
    PRINT 'Constraint CK_ProjectImplementationItemCheckLists_Procenat already exists.';
END
GO

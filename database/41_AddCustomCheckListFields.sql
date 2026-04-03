-- Add custom checklist item support and new fields to ProjectImplementationItemCheckLists
-- Custom items: CheckListItemId = -1 (dummy row), Opis stored directly on the row
-- New fields: DetaljanOpis (detailed description), PlaniraniRok (planned deadline)

-- 1. Insert dummy CheckListItem with Id = -1 (sentinel for custom items)
SET IDENTITY_INSERT [dbo].[CheckListItems] ON;

IF NOT EXISTS (SELECT 1 FROM [dbo].[CheckListItems] WHERE [Id] = -1)
BEGIN
    INSERT INTO [dbo].[CheckListItems] ([Id], [Opis])
    VALUES (-1, N'[Custom stavka]');
    PRINT 'Dummy CheckListItem Id=-1 inserted.';
END
ELSE
BEGIN
    PRINT 'Dummy CheckListItem Id=-1 already exists.';
END

SET IDENTITY_INSERT [dbo].[CheckListItems] OFF;
GO

-- 2. Add Opis column to ProjectImplementationItemCheckLists (for custom items)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ProjectImplementationItemCheckLists]')
      AND name = N'Opis'
)
BEGIN
    ALTER TABLE [dbo].[ProjectImplementationItemCheckLists]
    ADD [Opis] NVARCHAR(200) NULL;
    PRINT 'Column Opis added to ProjectImplementationItemCheckLists.';
END
ELSE
BEGIN
    PRINT 'Column Opis already exists.';
END
GO

-- 3. Add DetaljanOpis column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ProjectImplementationItemCheckLists]')
      AND name = N'DetaljanOpis'
)
BEGIN
    ALTER TABLE [dbo].[ProjectImplementationItemCheckLists]
    ADD [DetaljanOpis] NVARCHAR(1000) NULL;
    PRINT 'Column DetaljanOpis added to ProjectImplementationItemCheckLists.';
END
ELSE
BEGIN
    PRINT 'Column DetaljanOpis already exists.';
END
GO

-- 4. Add PlaniraniRok column
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ProjectImplementationItemCheckLists]')
      AND name = N'PlaniraniRok'
)
BEGIN
    ALTER TABLE [dbo].[ProjectImplementationItemCheckLists]
    ADD [PlaniraniRok] DATE NULL;
    PRINT 'Column PlaniraniRok added to ProjectImplementationItemCheckLists.';
END
ELSE
BEGIN
    PRINT 'Column PlaniraniRok already exists.';
END
GO

-- 5. Add Kompleksnost column (for custom items, overrides codebook value)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ProjectImplementationItemCheckLists]')
      AND name = N'Kompleksnost'
)
BEGIN
    ALTER TABLE [dbo].[ProjectImplementationItemCheckLists]
    ADD [Kompleksnost] DECIMAL(5,2) NULL;
    PRINT 'Column Kompleksnost added to ProjectImplementationItemCheckLists.';
END
ELSE
BEGIN
    PRINT 'Column Kompleksnost already exists.';
END
GO

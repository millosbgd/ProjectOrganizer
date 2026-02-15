-- Create CheckListItems and ImplementationItemCheckListItems tables
-- CheckListItems: Codebook for reusable checklist items
-- ImplementationItemCheckListItems: Junction table linking implementation items with checklist items

-- Create CheckListItems table (codebook)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CheckListItems')
BEGIN
    CREATE TABLE [dbo].[CheckListItems](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Opis] NVARCHAR(150) NOT NULL,
        CONSTRAINT [PK_CheckListItems] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'CheckListItems table created.';
END
ELSE
BEGIN
    PRINT 'CheckListItems table already exists.';
END
GO

-- Create ImplementationItemCheckListItems table (junction table)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImplementationItemCheckListItems')
BEGIN
    CREATE TABLE [dbo].[ImplementationItemCheckListItems](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ImplementationItemId] INT NOT NULL,
        [CheckListItemId] INT NOT NULL,
        CONSTRAINT [PK_ImplementationItemCheckListItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ImplementationItemCheckListItems_ImplementationItems] 
            FOREIGN KEY ([ImplementationItemId]) 
            REFERENCES [dbo].[ImplementationItems]([Id]) 
            ON DELETE CASCADE,
        CONSTRAINT [FK_ImplementationItemCheckListItems_CheckListItems] 
            FOREIGN KEY ([CheckListItemId]) 
            REFERENCES [dbo].[CheckListItems]([Id]) 
            ON DELETE RESTRICT
    );
    PRINT 'ImplementationItemCheckListItems table created.';
END
ELSE
BEGIN
    PRINT 'ImplementationItemCheckListItems table already exists.';
END
GO

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ImplementationItemCheckListItems_ImplementationItemId')
BEGIN
    CREATE INDEX IX_ImplementationItemCheckListItems_ImplementationItemId 
    ON ImplementationItemCheckListItems(ImplementationItemId);
    PRINT 'Index IX_ImplementationItemCheckListItems_ImplementationItemId created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ImplementationItemCheckListItems_CheckListItemId')
BEGIN
    CREATE INDEX IX_ImplementationItemCheckListItems_CheckListItemId 
    ON ImplementationItemCheckListItems(CheckListItemId);
    PRINT 'Index IX_ImplementationItemCheckListItems_CheckListItemId created.';
END
GO

-- Insert sample data for CheckListItems (if needed)
IF NOT EXISTS (SELECT * FROM CheckListItems)
BEGIN
    INSERT INTO CheckListItems (Opis) VALUES
    ('Analiza zahteva'),
    ('Izrada specifikacije'),
    ('Dizajn baze podataka'),
    ('Dizajn API-ja'),
    ('Implementacija backend-a'),
    ('Implementacija frontend-a'),
    ('Pisanje testova'),
    ('Code review'),
    ('Dokumentacija'),
    ('Deployment'),
    ('User acceptance testing'),
    ('Performance testing'),
    ('Security audit'),
    ('Bug fixing'),
    ('Monitoring setup');
    PRINT 'Sample check list items inserted.';
END
GO

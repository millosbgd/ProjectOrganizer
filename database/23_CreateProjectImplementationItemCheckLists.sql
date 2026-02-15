-- Create ProjectImplementationItemCheckLists table
-- Tracks checklist items for project implementation items with completion status

-- Create ProjectImplementationItemCheckLists table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProjectImplementationItemCheckLists')
BEGIN
    CREATE TABLE [dbo].[ProjectImplementationItemCheckLists](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ProjectImplementationItemId] INT NOT NULL,
        [CheckListItemId] INT NOT NULL,
        [Zavrsen] BIT NOT NULL DEFAULT 0,
        [ZavrsenDatum] DATETIME NULL,
        CONSTRAINT [PK_ProjectImplementationItemCheckLists] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProjectImplementationItemCheckLists_ProjectImplementationItems] 
            FOREIGN KEY ([ProjectImplementationItemId]) 
            REFERENCES [dbo].[ProjectImplementationItems]([Id]) 
            ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectImplementationItemCheckLists_CheckListItems] 
            FOREIGN KEY ([CheckListItemId]) 
            REFERENCES [dbo].[CheckListItems]([Id]) 
            ON DELETE RESTRICT
    );
    PRINT 'ProjectImplementationItemCheckLists table created.';
END
ELSE
BEGIN
    PRINT 'ProjectImplementationItemCheckLists table already exists.';
END
GO

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProjectImplementationItemCheckLists_ProjectImplementationItemId')
BEGIN
    CREATE INDEX IX_ProjectImplementationItemCheckLists_ProjectImplementationItemId 
    ON ProjectImplementationItemCheckLists(ProjectImplementationItemId);
    PRINT 'Index IX_ProjectImplementationItemCheckLists_ProjectImplementationItemId created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProjectImplementationItemCheckLists_CheckListItemId')
BEGIN
    CREATE INDEX IX_ProjectImplementationItemCheckLists_CheckListItemId 
    ON ProjectImplementationItemCheckLists(CheckListItemId);
    PRINT 'Index IX_ProjectImplementationItemCheckLists_CheckListItemId created.';
END
GO

-- Create composite index for common query pattern (ProjectItem + Completion status)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProjectImplementationItemCheckLists_ProjectItem_Zavrsen')
BEGIN
    CREATE INDEX IX_ProjectImplementationItemCheckLists_ProjectItem_Zavrsen 
    ON ProjectImplementationItemCheckLists(ProjectImplementationItemId, Zavrsen);
    PRINT 'Index IX_ProjectImplementationItemCheckLists_ProjectItem_Zavrsen created.';
END
GO

PRINT 'Migration 23_CreateProjectImplementationItemCheckLists completed successfully.';

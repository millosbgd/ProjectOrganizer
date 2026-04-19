USE ProjectOrganizer;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DevOpsUsers')
BEGIN
    CREATE TABLE DevOpsUsers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UniqueName NVARCHAR(300) NOT NULL,
        DisplayName NVARCHAR(300) NOT NULL,
        Organization NVARCHAR(200) NOT NULL,
        ImageUrl NVARCHAR(500) NULL,
        SyncedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_DevOpsUsers_UniqueName_Org UNIQUE (UniqueName, Organization)
    );

    CREATE NONCLUSTERED INDEX IX_DevOpsUsers_Organization
        ON DevOpsUsers(Organization);
END
GO

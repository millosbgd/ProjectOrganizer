CREATE TABLE dbo.UserMenuPermissions
(
    Id INT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    MenuKey NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserMenuPermissions_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_UserMenuPermissions PRIMARY KEY (Id),

    CONSTRAINT FK_UserMenuPermissions_Users_UserId
        FOREIGN KEY (UserId)
        REFERENCES dbo.Users (Id)
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_UserMenuPermissions_UserId_MenuKey
ON dbo.UserMenuPermissions (UserId, MenuKey);

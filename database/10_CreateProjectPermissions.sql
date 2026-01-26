-- Create ProjectPermissions table
CREATE TABLE ProjectPermissions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ProjekatId INT NOT NULL,
    PermissionLevel NVARCHAR(50) NOT NULL DEFAULT 'Read',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy INT NULL,
    CONSTRAINT FK_ProjectPermissions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProjectPermissions_Projekti FOREIGN KEY (ProjekatId) REFERENCES Projekti(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_ProjectPermissions_UserProjekat UNIQUE (UserId, ProjekatId)
);
GO

-- Create indexes for faster lookups
CREATE INDEX IX_ProjectPermissions_UserId ON ProjectPermissions(UserId);
GO

CREATE INDEX IX_ProjectPermissions_ProjekatId ON ProjectPermissions(ProjekatId);
GO

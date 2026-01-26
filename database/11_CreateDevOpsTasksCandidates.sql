-- Create DevOpsTasksCandidates table
CREATE TABLE DevOpsTasksCandidates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AktivnostId INT NOT NULL,
    GeneratedContent NVARCHAR(MAX) NOT NULL,
    UserId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DevOpsTasksCandidates_Aktivnosti FOREIGN KEY (AktivnostId) REFERENCES Aktivnosti(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DevOpsTasksCandidates_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- Create index on AktivnostId for faster lookups
CREATE INDEX IX_DevOpsTasksCandidates_AktivnostId ON DevOpsTasksCandidates(AktivnostId);
GO

-- Create index on UserId
CREATE INDEX IX_DevOpsTasksCandidates_UserId ON DevOpsTasksCandidates(UserId);
GO

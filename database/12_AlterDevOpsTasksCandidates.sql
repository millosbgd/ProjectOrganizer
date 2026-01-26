-- Drop existing DevOpsTasksCandidates table and recreate with new structure
-- First, drop foreign key constraints
IF OBJECT_ID('FK_DevOpsTasksCandidates_Aktivnosti', 'F') IS NOT NULL
    ALTER TABLE DevOpsTasksCandidates DROP CONSTRAINT FK_DevOpsTasksCandidates_Aktivnosti;
GO

IF OBJECT_ID('FK_DevOpsTasksCandidates_Users', 'F') IS NOT NULL
    ALTER TABLE DevOpsTasksCandidates DROP CONSTRAINT FK_DevOpsTasksCandidates_Users;
GO

-- Drop indexes
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DevOpsTasksCandidates_AktivnostId')
    DROP INDEX IX_DevOpsTasksCandidates_AktivnostId ON DevOpsTasksCandidates;
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DevOpsTasksCandidates_UserId')
    DROP INDEX IX_DevOpsTasksCandidates_UserId ON DevOpsTasksCandidates;
GO

-- Drop table
IF OBJECT_ID('DevOpsTasksCandidates', 'U') IS NOT NULL
    DROP TABLE DevOpsTasksCandidates;
GO

-- Recreate table with new structure
CREATE TABLE DevOpsTasksCandidates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AktivnostId INT NOT NULL,
    UserId INT NOT NULL,
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    AcceptanceCriteria NVARCHAR(MAX) NULL,
    Priority NVARCHAR(50) NULL,
    Estimation NVARCHAR(50) NULL,
    OrderIndex INT NOT NULL DEFAULT 0,
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

-- Create index on OrderIndex for sorting
CREATE INDEX IX_DevOpsTasksCandidates_OrderIndex ON DevOpsTasksCandidates(OrderIndex);
GO

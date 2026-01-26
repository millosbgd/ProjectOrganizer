-- Create UserSettings table
CREATE TABLE UserSettings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(255) NOT NULL UNIQUE,
    OpenAiApiKey NVARCHAR(500) NULL,
    OpenAiModel NVARCHAR(50) NOT NULL DEFAULT 'gpt-4o-mini',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Create index on UserId for faster lookups
CREATE INDEX IX_UserSettings_UserId ON UserSettings(UserId);
GO

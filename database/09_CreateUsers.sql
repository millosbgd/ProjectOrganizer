-- Create Users table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Auth0Id NVARCHAR(255) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL,
    Name NVARCHAR(255) NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'User',
    IsActive BIT NOT NULL DEFAULT 1,
    LastLogin DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Create index on Auth0Id for faster lookups
CREATE INDEX IX_Users_Auth0Id ON Users(Auth0Id);
GO

-- Create index on Email for search
CREATE INDEX IX_Users_Email ON Users(Email);
GO

-- Create Codebooks table for storing all codebook entries
CREATE TABLE Codebooks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Type NVARCHAR(50) NOT NULL,
    Code NVARCHAR(100) NOT NULL,
    Value NVARCHAR(200) NOT NULL,
    OrderIndex INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT UQ_Codebooks_Type_Code UNIQUE (Type, Code)
);

CREATE INDEX IX_Codebooks_Type ON Codebooks(Type);
CREATE INDEX IX_Codebooks_Type_Active ON Codebooks(Type, IsActive);
GO

-- Seed country data
INSERT INTO Codebooks (Type, Code, Value, OrderIndex) VALUES
-- Ex-YU region
('Country', 'RS', 'Srbija', 1),
('Country', 'ME', 'Crna Gora', 2),
('Country', 'BA', 'Bosna i Hercegovina', 3),
('Country', 'HR', 'Hrvatska', 4),
('Country', 'SI', 'Slovenija', 5),
('Country', 'MK', 'Severna Makedonija', 6),
-- Western Europe
('Country', 'AT', 'Austrija', 10),
('Country', 'DE', 'Nemačka', 11),
('Country', 'CH', 'Švajcarska', 12),
('Country', 'IT', 'Italija', 13),
('Country', 'FR', 'Francuska', 14),
('Country', 'GB', 'Velika Britanija', 15),
('Country', 'ES', 'Španija', 16),
('Country', 'PT', 'Portugalija', 17),
-- Eastern Europe
('Country', 'GR', 'Grčka', 20),
('Country', 'BG', 'Bugarska', 21),
('Country', 'RO', 'Rumunija', 22),
('Country', 'PL', 'Poljska', 23),
('Country', 'CZ', 'Češka', 24),
('Country', 'SK', 'Slovačka', 25),
('Country', 'HU', 'Mađarska', 26),
-- Northern Europe
('Country', 'NL', 'Holandija', 30),
('Country', 'BE', 'Belgija', 31),
('Country', 'DK', 'Danska', 32),
('Country', 'SE', 'Švedska', 33),
('Country', 'NO', 'Norveška', 34),
('Country', 'FI', 'Finska', 35),
-- Other
('Country', 'OTHER', 'Ostalo', 99);
GO

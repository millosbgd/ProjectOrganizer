-- Create DocumentNumbering table for automatic document numbering
CREATE TABLE DocumentNumbering (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Year INT NOT NULL,
    DocumentType NVARCHAR(50) NOT NULL,
    LastNumber INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_DocumentNumbering_Year_Type UNIQUE (Year, DocumentType)
);

-- Insert initial record for Projekat in 2025
INSERT INTO DocumentNumbering (Year, DocumentType, LastNumber)
VALUES (2025, 'Projekat', 0);

GO

-- Kreiranje tabele za beleške vezane za projekte
CREATE TABLE Notes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjekatId INT NOT NULL,
    Opis NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Notes_Projekti FOREIGN KEY (ProjekatId) 
        REFERENCES Projekti(Id) ON DELETE CASCADE
);

-- Index za brže pretraživanje notes po projektu
CREATE INDEX IX_Notes_ProjekatId ON Notes(ProjekatId);

GO

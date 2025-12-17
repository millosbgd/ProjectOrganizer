-- Kreiranje tabele za dokumente vezane za projekte
CREATE TABLE Dokumenti (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjekatId INT NOT NULL,
    NazivFajla NVARCHAR(500) NOT NULL,
    TipFajla NVARCHAR(50) NOT NULL,
    BlobUrl NVARCHAR(1000) NOT NULL,
    Velicina BIGINT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Dokumenti_Projekti FOREIGN KEY (ProjekatId) 
        REFERENCES Projekti(Id) ON DELETE CASCADE
);

-- Index za brže pretraživanje dokumenata po projektu
CREATE INDEX IX_Dokumenti_ProjekatId ON Dokumenti(ProjekatId);

GO

USE ProjectOrganizer;
GO

-- Klijenti tabela
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Klijenti')
BEGIN
    CREATE TABLE Klijenti (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Naziv NVARCHAR(200) NOT NULL,
        Adresa NVARCHAR(300),
        Grad NVARCHAR(100),
        Zemlja NVARCHAR(100),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE()
    );
END
GO

-- Projekti tabela
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projekti')
BEGIN
    CREATE TABLE Projekti (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BrojProjekta NVARCHAR(50) NOT NULL UNIQUE,
        Datum DATE NOT NULL,
        Naziv NVARCHAR(300) NOT NULL,
        Aktivan BIT NOT NULL DEFAULT 1,
        Status NVARCHAR(50) NOT NULL,
        KlijentId INT NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Projekti_Klijenti FOREIGN KEY (KlijentId) REFERENCES Klijenti(Id)
    );
END
GO

-- Aktivnosti tabela
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Aktivnosti')
BEGIN
    CREATE TABLE Aktivnosti (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Opis NVARCHAR(MAX) NOT NULL,
        Datum DATE NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Vrsta NVARCHAR(100) NOT NULL,
        ProjekatId INT NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Aktivnosti_Projekti FOREIGN KEY (ProjekatId) REFERENCES Projekti(Id) ON DELETE CASCADE
    );
END
GO

-- Indexi za bolje performanse
CREATE NONCLUSTERED INDEX IX_Projekti_KlijentId ON Projekti(KlijentId);
CREATE NONCLUSTERED INDEX IX_Projekti_Status ON Projekti(Status);
CREATE NONCLUSTERED INDEX IX_Aktivnosti_ProjekatId ON Aktivnosti(ProjekatId);
CREATE NONCLUSTERED INDEX IX_Aktivnosti_Status ON Aktivnosti(Status);
GO

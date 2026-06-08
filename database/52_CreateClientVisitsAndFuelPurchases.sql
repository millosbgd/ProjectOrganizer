CREATE TABLE dbo.ClientVisits
(
    Id INT IDENTITY(1,1) NOT NULL,
    KlijentId INT NOT NULL,
    AktivnostId INT NOT NULL,
    Grad NVARCHAR(100) NOT NULL,
    Kilometraza DECIMAL(10,2) NOT NULL,
    GorivoLitara DECIMAL(10,2) NOT NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ClientVisits_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ClientVisits_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ClientVisits PRIMARY KEY (Id),
    CONSTRAINT FK_ClientVisits_Klijenti_KlijentId
        FOREIGN KEY (KlijentId) REFERENCES dbo.Klijenti (Id),
    CONSTRAINT FK_ClientVisits_Aktivnosti_AktivnostId
        FOREIGN KEY (AktivnostId) REFERENCES dbo.Aktivnosti (Id),
    CONSTRAINT FK_ClientVisits_Users_CreatedBy
        FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (Id)
        ON DELETE SET NULL
);

CREATE INDEX IX_ClientVisits_KlijentId ON dbo.ClientVisits (KlijentId);
CREATE INDEX IX_ClientVisits_AktivnostId ON dbo.ClientVisits (AktivnostId);
CREATE INDEX IX_ClientVisits_CreatedBy ON dbo.ClientVisits (CreatedBy);

CREATE TABLE dbo.FuelPurchases
(
    Id INT IDENTITY(1,1) NOT NULL,
    Datum DATETIME2 NOT NULL,
    Kolicina DECIMAL(10,2) NOT NULL,
    JedinicnaCena DECIMAL(18,2) NOT NULL,
    UkupnaCena DECIMAL(18,2) NOT NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_FuelPurchases_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_FuelPurchases_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_FuelPurchases PRIMARY KEY (Id),
    CONSTRAINT FK_FuelPurchases_Users_CreatedBy
        FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (Id)
        ON DELETE SET NULL
);

CREATE INDEX IX_FuelPurchases_Datum ON dbo.FuelPurchases (Datum);
CREATE INDEX IX_FuelPurchases_CreatedBy ON dbo.FuelPurchases (CreatedBy);

-- Kreiranje tabele za Šablone za podršku
-- Ručno izvršavanje na bazi

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SabloniPodrske', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SabloniPodrske
    (
        Id int IDENTITY(1,1) NOT NULL,
        KlijentId int NULL,
        Naslov nvarchar(2000) NOT NULL,
        OpisZahteva nvarchar(2000) NOT NULL CONSTRAINT DF_SabloniPodrske_OpisZahteva DEFAULT(N''),
        OpisResenja nvarchar(2000) NOT NULL,
        OdgovorKlijentu nvarchar(2000) NOT NULL CONSTRAINT DF_SabloniPodrske_OdgovorKlijentu DEFAULT(N''),
        Kreirao int NULL,
        Promenio int NULL,
        VremeKreiranja datetime2 NOT NULL,
        VremePromene datetime2 NOT NULL,

        CONSTRAINT PK_SabloniPodrske PRIMARY KEY (Id),
        CONSTRAINT FK_SabloniPodrske_Klijenti_KlijentId
            FOREIGN KEY (KlijentId) REFERENCES dbo.Klijenti(Id) ON DELETE SET NULL,
        CONSTRAINT FK_SabloniPodrske_Users_Kreirao
            FOREIGN KEY (Kreirao) REFERENCES dbo.Users(Id) ON DELETE SET NULL,
        CONSTRAINT FK_SabloniPodrske_Users_Promenio
            FOREIGN KEY (Promenio) REFERENCES dbo.Users(Id) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SabloniPodrske_KlijentId' AND object_id = OBJECT_ID(N'dbo.SabloniPodrske'))
    CREATE INDEX IX_SabloniPodrske_KlijentId ON dbo.SabloniPodrske(KlijentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SabloniPodrske_Kreirao' AND object_id = OBJECT_ID(N'dbo.SabloniPodrske'))
    CREATE INDEX IX_SabloniPodrske_Kreirao ON dbo.SabloniPodrske(Kreirao);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SabloniPodrske_Promenio' AND object_id = OBJECT_ID(N'dbo.SabloniPodrske'))
    CREATE INDEX IX_SabloniPodrske_Promenio ON dbo.SabloniPodrske(Promenio);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SabloniPodrske_VremeKreiranja' AND object_id = OBJECT_ID(N'dbo.SabloniPodrske'))
    CREATE INDEX IX_SabloniPodrske_VremeKreiranja ON dbo.SabloniPodrske(VremeKreiranja);

IF COL_LENGTH(N'dbo.SabloniPodrske', N'Naslov') IS NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske
        ADD Naslov nvarchar(2000) NOT NULL CONSTRAINT DF_SabloniPodrske_Naslov DEFAULT(N'');
END;

IF COL_LENGTH(N'dbo.SabloniPodrske', N'Naslov') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske ALTER COLUMN Naslov nvarchar(2000) NOT NULL;
END;

IF COL_LENGTH(N'dbo.SabloniPodrske', N'OpisZahteva') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske ALTER COLUMN OpisZahteva nvarchar(2000) NOT NULL;
END;

IF COL_LENGTH(N'dbo.SabloniPodrske', N'OpisResenja') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske ALTER COLUMN OpisResenja nvarchar(2000) NOT NULL;
END;

IF COL_LENGTH(N'dbo.SabloniPodrske', N'OdgovorKlijentu') IS NOT NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske ALTER COLUMN OdgovorKlijentu nvarchar(2000) NOT NULL;
END;

IF OBJECT_ID(N'DF_SabloniPodrske_OpisZahteva', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske
        ADD CONSTRAINT DF_SabloniPodrske_OpisZahteva DEFAULT(N'') FOR OpisZahteva;
END;

IF OBJECT_ID(N'DF_SabloniPodrske_OdgovorKlijentu', N'D') IS NULL
BEGIN
    ALTER TABLE dbo.SabloniPodrske
        ADD CONSTRAINT DF_SabloniPodrske_OdgovorKlijentu DEFAULT(N'') FOR OdgovorKlijentu;
END;

IF OBJECT_ID(N'dbo.UserMenuPermissions', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.UserMenuPermissions (UserId, MenuKey, CreatedAt)
    SELECT source.UserId, N'sabloni-podrske', SYSUTCDATETIME()
    FROM dbo.UserMenuPermissions source
    WHERE source.MenuKey = N'aktivnosti'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.UserMenuPermissions existing
          WHERE existing.UserId = source.UserId
            AND existing.MenuKey = N'sabloni-podrske'
      );
END;

COMMIT TRANSACTION;

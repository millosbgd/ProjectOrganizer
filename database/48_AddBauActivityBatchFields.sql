IF COL_LENGTH('dbo.Aktivnosti', 'KlijentId') IS NULL
BEGIN
    ALTER TABLE dbo.Aktivnosti
    ADD KlijentId INT NULL;
END;

IF COL_LENGTH('dbo.Aktivnosti', 'BauTipAktivnosti') IS NULL
BEGIN
    ALTER TABLE dbo.Aktivnosti
    ADD BauTipAktivnosti NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.Aktivnosti', 'BauTrajanjeMinuta') IS NULL
BEGIN
    ALTER TABLE dbo.Aktivnosti
    ADD BauTrajanjeMinuta INT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Aktivnosti_KlijentId'
      AND object_id = OBJECT_ID('dbo.Aktivnosti')
)
BEGIN
    CREATE INDEX IX_Aktivnosti_KlijentId ON dbo.Aktivnosti(KlijentId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Aktivnosti_Klijenti_KlijentId'
)
BEGIN
    ALTER TABLE dbo.Aktivnosti
    ADD CONSTRAINT FK_Aktivnosti_Klijenti_KlijentId
        FOREIGN KEY (KlijentId) REFERENCES dbo.Klijenti(Id)
        ON DELETE SET NULL;
END;

DECLARE @EntityTypeId INT;

SELECT @EntityTypeId = Id
FROM dbo.CodebookEntities
WHERE Name = 'BauActivityType';

IF @EntityTypeId IS NULL
BEGIN
    INSERT INTO dbo.CodebookEntities (Name, Description, IsActive, CreatedAt)
    VALUES ('BauActivityType', 'Tipovi BAU aktivnosti za zbirni unos', 1, SYSUTCDATETIME());

    SET @EntityTypeId = SCOPE_IDENTITY();
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'SUPPORT')
BEGIN
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'SUPPORT', 'Podrška', 10, 1, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'CONSULTING')
BEGIN
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'CONSULTING', 'Konsultacije', 20, 1, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'ANALYSIS')
BEGIN
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'ANALYSIS', 'Analiza', 30, 1, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'ADMIN')
BEGIN
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'ADMIN', 'Administracija', 40, 1, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Codebooks WHERE EntityTypeId = @EntityTypeId AND Code = 'COMMUNICATION')
BEGIN
    INSERT INTO dbo.Codebooks (EntityTypeId, Code, Value, OrderIndex, IsActive, CreatedAt)
    VALUES (@EntityTypeId, 'COMMUNICATION', 'Komunikacija', 50, 1, SYSUTCDATETIME());
END;

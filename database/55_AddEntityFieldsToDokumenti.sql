-- Dodaje genericko vezivanje dokumenata za entitete
-- ProjekatId ostaje privremeno zbog kompatibilnosti sa postojecim kodom/podacima

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Dokumenti', N'Entity') IS NULL
BEGIN
    ALTER TABLE dbo.Dokumenti
        ADD Entity nvarchar(50) NOT NULL
            CONSTRAINT DF_Dokumenti_Entity DEFAULT(N'Projekat');
END;

IF COL_LENGTH(N'dbo.Dokumenti', N'EntityId') IS NULL
BEGIN
    ALTER TABLE dbo.Dokumenti
        ADD EntityId int NULL;
END;

UPDATE dbo.Dokumenti
SET
    Entity = N'Projekat',
    EntityId = ProjekatId
WHERE EntityId IS NULL
  AND ProjekatId IS NOT NULL;

IF EXISTS
(
    SELECT 1
    FROM dbo.Dokumenti
    WHERE EntityId IS NULL
)
BEGIN
    THROW 51000, 'Dokumenti.EntityId ne moze biti NULL. Proverite postojece zapise bez ProjekatId.', 1;
END;

IF COL_LENGTH(N'dbo.Dokumenti', N'EntityId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Dokumenti
        ALTER COLUMN EntityId int NOT NULL;
END;

IF COL_LENGTH(N'dbo.Dokumenti', N'ProjekatId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Dokumenti
        ALTER COLUMN ProjekatId int NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Dokumenti_Entity_EntityId'
      AND object_id = OBJECT_ID(N'dbo.Dokumenti')
)
BEGIN
    CREATE INDEX IX_Dokumenti_Entity_EntityId
        ON dbo.Dokumenti(Entity, EntityId);
END;

COMMIT TRANSACTION;
GO

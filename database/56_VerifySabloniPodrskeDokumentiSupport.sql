-- Provera podrske za dokumente na SablonPodrske
-- Nema nove seme: koristi se genericki dbo.Dokumenti(Entity, EntityId) model iz skripte 55.

IF OBJECT_ID(N'dbo.SabloniPodrske', N'U') IS NULL
BEGIN
    THROW 51000, 'Tabela dbo.SabloniPodrske ne postoji.', 1;
END;

IF OBJECT_ID(N'dbo.Dokumenti', N'U') IS NULL
BEGIN
    THROW 51001, 'Tabela dbo.Dokumenti ne postoji.', 1;
END;

IF COL_LENGTH(N'dbo.Dokumenti', N'Entity') IS NULL
BEGIN
    THROW 51002, 'Kolona dbo.Dokumenti.Entity ne postoji. Prvo izvrsite database/55_AddEntityFieldsToDokumenti.sql.', 1;
END;

IF COL_LENGTH(N'dbo.Dokumenti', N'EntityId') IS NULL
BEGIN
    THROW 51003, 'Kolona dbo.Dokumenti.EntityId ne postoji. Prvo izvrsite database/55_AddEntityFieldsToDokumenti.sql.', 1;
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

PRINT 'Dokumenti za SablonPodrske su podrzani preko dbo.Dokumenti(Entity = ''SablonPodrske'', EntityId = SabloniPodrske.Id).';
GO

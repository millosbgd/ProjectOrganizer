USE ProjectOrganizer;
GO

-- Korak 1: Dodaj entitet tipa 2 = DevOps uloge
IF NOT EXISTS (SELECT * FROM CodebookEntities WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT CodebookEntities ON;
    INSERT INTO CodebookEntities (Id, Name, Description, IsActive, CreatedAt)
    VALUES (2, 'DevOpsRole', 'Uloge DevOps korisnika', 1, GETDATE());
    SET IDENTITY_INSERT CodebookEntities OFF;
END
GO

-- Korak 2: Ubaci vrednosti šifarnika za DevOps uloge
IF NOT EXISTS (SELECT * FROM Codebooks WHERE EntityTypeId = 2 AND Code = 'TL')
BEGIN
    INSERT INTO Codebooks (EntityTypeId, Type, Code, Value, OrderIndex, IsActive)
    VALUES
        (2, 'DevOpsRole', 'TL',         'Team Leader', 1, 1),
        (2, 'DevOpsRole', 'KONSULTANT', 'Konsultant',  2, 1),
        (2, 'DevOpsRole', 'PROGRAMER',  'Programer',   3, 1);
END
GO

-- Korak 3: Dodaj RoleId kolonu u DevOpsUsers
IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID('DevOpsUsers') AND name = 'RoleId'
)
BEGIN
    ALTER TABLE DevOpsUsers
    ADD RoleId INT NULL
        CONSTRAINT FK_DevOpsUsers_Role FOREIGN KEY REFERENCES Codebooks(Id);
END
GO

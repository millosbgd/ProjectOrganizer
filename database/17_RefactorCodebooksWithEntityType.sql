-- 17_RefactorCodebooksWithEntityType.sql
-- Refaktorisanje šifarnika da koristi CodebookEntities tabelu

-- Korak 1: Kreiranje tabele CodebookEntities
CREATE TABLE CodebookEntities (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_CodebookEntities_Name UNIQUE (Name)
);

-- Korak 2: Popunjavanje CodebookEntities sa postojećim tipovima iz Codebooks
INSERT INTO CodebookEntities (Name, IsActive, CreatedAt)
SELECT DISTINCT Type, 1, GETDATE()
FROM Codebooks
WHERE Type IS NOT NULL
ORDER BY Type;

-- Korak 3: Dodavanje nove kolone EntityTypeId u Codebooks
ALTER TABLE Codebooks
ADD EntityTypeId INT NULL;

-- Korak 4: Popunjavanje EntityTypeId na osnovu Type kolone
UPDATE c
SET c.EntityTypeId = ce.Id
FROM Codebooks c
INNER JOIN CodebookEntities ce ON c.Type = ce.Name;

-- Korak 5: Promena EntityTypeId da bude NOT NULL
ALTER TABLE Codebooks
ALTER COLUMN EntityTypeId INT NOT NULL;

-- Korak 6: Brisanje svih constrainta i indeksa koji koriste Type kolonu
-- Brisanje unique constrainta
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Codebooks_Type_Code' AND object_id = OBJECT_ID('Codebooks'))
    ALTER TABLE Codebooks DROP CONSTRAINT UQ_Codebooks_Type_Code;

-- Brisanje svih indeksa na Type koloni
DECLARE @sql NVARCHAR(MAX) = '';

SELECT @sql = @sql + 'DROP INDEX ' + QUOTENAME(i.name) + ' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) + '.' + QUOTENAME(OBJECT_NAME(i.object_id)) + ';' + CHAR(13)
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE c.name = 'Type' 
  AND OBJECT_NAME(i.object_id) = 'Codebooks'
  AND i.is_primary_key = 0
  AND i.is_unique_constraint = 0;

IF @sql <> ''
BEGIN
    EXEC sp_executesql @sql;
    PRINT 'Dropped indexes on Type column';
END

-- Korak 7: Kreiranje novih indeksa sa EntityTypeId
CREATE NONCLUSTERED INDEX IX_Codebooks_EntityTypeId 
    ON Codebooks(EntityTypeId);

CREATE NONCLUSTERED INDEX IX_Codebooks_EntityTypeId_IsActive 
    ON Codebooks(EntityTypeId, IsActive);

CREATE UNIQUE NONCLUSTERED INDEX IX_Codebooks_EntityTypeId_Code 
    ON Codebooks(EntityTypeId, Code);

-- Korak 8: Dodavanje Foreign Key constrainta
ALTER TABLE Codebooks
ADD CONSTRAINT FK_Codebooks_CodebookEntities_EntityTypeId 
    FOREIGN KEY (EntityTypeId) 
    REFERENCES CodebookEntities(Id)
    ON DELETE NO ACTION;

-- Korak 9: Brisanje stare Type kolone
ALTER TABLE Codebooks
DROP COLUMN Type;

PRINT 'Codebooks refactoring completed successfully!';

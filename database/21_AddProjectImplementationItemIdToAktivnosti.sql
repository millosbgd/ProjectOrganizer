-- 21_AddProjectImplementationItemIdToAktivnosti.sql
-- Dodavanje kolone ProjectImplementationItemId u tabelu Aktivnosti

-- Dodavanje kolone ProjectImplementationItemId kao nullable INT
ALTER TABLE Aktivnosti
ADD ProjectImplementationItemId INT NULL;

-- Kreiranje indeksa za lakše pretragu
CREATE NONCLUSTERED INDEX IX_Aktivnosti_ProjectImplementationItemId
    ON Aktivnosti(ProjectImplementationItemId);

-- Dodavanje Foreign Key constrainta
ALTER TABLE Aktivnosti
ADD CONSTRAINT FK_Aktivnosti_ProjectImplementationItems 
    FOREIGN KEY (ProjectImplementationItemId) 
    REFERENCES ProjectImplementationItems(Id)
    ON DELETE SET NULL;

PRINT 'ProjectImplementationItemId column added to Aktivnosti table successfully!';

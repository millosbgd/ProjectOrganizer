-- 18_AddCreatedByToProjekti.sql
-- Dodavanje kolone CreatedBy u tabelu Projekti

-- Dodavanje kolone CreatedBy kao INT (User ID)
ALTER TABLE Projekti
ADD CreatedBy INT NULL;

-- Kreiranje indeksa za lakše pretragu po kreatoru
CREATE NONCLUSTERED INDEX IX_Projekti_CreatedBy
    ON Projekti(CreatedBy);

-- Dodavanje Foreign Key constrainta
ALTER TABLE Projekti
ADD CONSTRAINT FK_Projekti_Users_CreatedBy 
    FOREIGN KEY (CreatedBy) 
    REFERENCES Users(Id)
    ON DELETE SET NULL;

PRINT 'CreatedBy column added to Projekti table successfully!';

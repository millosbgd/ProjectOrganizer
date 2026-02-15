-- Add BAU (Business As Usual) support to Aktivnosti table
-- BAU activities are not tied to a specific project (ProjekatId = NULL)

-- Add Bau column (default false - activities are project-related by default)
ALTER TABLE Aktivnosti
ADD Bau BIT NOT NULL DEFAULT 0;

-- Make ProjekatId nullable for BAU activities
ALTER TABLE Aktivnosti
ALTER COLUMN ProjekatId INT NULL;

-- For existing activities without a valid project, mark them as BAU
UPDATE Aktivnosti
SET Bau = 1, ProjekatId = NULL
WHERE ProjekatId IS NULL OR NOT EXISTS (SELECT 1 FROM Projekti WHERE Id = Aktivnosti.ProjekatId);

-- For existing activities with valid projects, ensure Bau = 0
UPDATE Aktivnosti
SET Bau = 0
WHERE EXISTS (SELECT 1 FROM Projekti WHERE Id = Aktivnosti.ProjekatId);

-- Create index for BAU filtering
CREATE INDEX IX_Aktivnosti_Bau ON Aktivnosti(Bau);

-- Create composite index for common query pattern (User + BAU activities)
CREATE INDEX IX_Aktivnosti_CreatedBy_Bau 
ON Aktivnosti(CreatedBy, Bau)
WHERE Bau = 1;

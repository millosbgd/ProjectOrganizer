-- Add CreatedBy column to Aktivnosti table to track activity ownership
-- This enables filtering activities by user in the calendar view

-- Add CreatedBy column (nullable initially)
ALTER TABLE Aktivnosti
ADD CreatedBy INT NULL;

-- Set CreatedBy for existing activities based on their project's creator
UPDATE Aktivnosti
SET CreatedBy = p.CreatedBy
FROM Aktivnosti a
INNER JOIN Projekti p ON a.ProjekatId = p.Id
WHERE a.CreatedBy IS NULL;

-- Add foreign key constraint
ALTER TABLE Aktivnosti
ADD CONSTRAINT FK_Aktivnosti_CreatedBy_Users 
FOREIGN KEY (CreatedBy) REFERENCES Users(Id);

-- Create index for better query performance
CREATE INDEX IX_Aktivnosti_CreatedBy ON Aktivnosti(CreatedBy);

-- Create composite index for common calendar query pattern (CreatedBy + Time range)
CREATE INDEX IX_Aktivnosti_CreatedBy_TimeRange 
ON Aktivnosti(CreatedBy, StartUtc, EndUtc)
WHERE StartUtc IS NOT NULL AND EndUtc IS NOT NULL;

-- Add DevOps configuration fields to Projekti table
ALTER TABLE Projekti ADD DevOpsOrganization NVARCHAR(200) NULL;
GO

ALTER TABLE Projekti ADD DevOpsProject NVARCHAR(200) NULL;
GO

-- Optional: Add area path and iteration path for more precise targeting
ALTER TABLE Projekti ADD DevOpsAreaPath NVARCHAR(500) NULL;
GO

ALTER TABLE Projekti ADD DevOpsIterationPath NVARCHAR(500) NULL;
GO

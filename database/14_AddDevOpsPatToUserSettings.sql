-- Add DevOps Personal Access Token to UserSettings table
ALTER TABLE UserSettings ADD DevOpsPersonalAccessToken NVARCHAR(500) NULL;
GO

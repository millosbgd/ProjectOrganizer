IF COL_LENGTH('dbo.DevOpsTaskStatusHistory', 'EndedAt') IS NOT NULL
BEGIN
    ALTER TABLE dbo.DevOpsTaskStatusHistory
        DROP COLUMN EndedAt;
END;
GO

IF COL_LENGTH('dbo.DevOpsTaskStatusHistory', 'StartedAt') IS NOT NULL
BEGIN
    ALTER TABLE dbo.DevOpsTaskStatusHistory
        DROP COLUMN StartedAt;
END;
GO

IF COL_LENGTH('dbo.DevOpsTasksCandidates', 'DevOpsState') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTasksCandidates
    ADD DevOpsState NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.DevOpsTasksCandidates', 'DevOpsAssignedTo') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTasksCandidates
    ADD DevOpsAssignedTo NVARCHAR(255) NULL;
END;

IF COL_LENGTH('dbo.DevOpsTasksCandidates', 'DevOpsChangedDate') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTasksCandidates
    ADD DevOpsChangedDate DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.DevOpsTasksCandidates', 'LastDevOpsSyncAt') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTasksCandidates
    ADD LastDevOpsSyncAt DATETIME2 NULL;
END;

IF COL_LENGTH('dbo.DevOpsTasksCandidates', 'LastDevOpsSyncStatus') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTasksCandidates
    ADD LastDevOpsSyncStatus NVARCHAR(50) NULL;
END;

IF COL_LENGTH('dbo.DevOpsTasksCandidates', 'LastDevOpsSyncError') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTasksCandidates
    ADD LastDevOpsSyncError NVARCHAR(1000) NULL;
END;

USE ProjectOrganizer;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DevOpsTaskStatusHistory')
BEGIN
    CREATE TABLE DevOpsTaskStatusHistory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DevOpsTaskCandidateId INT NOT NULL,
        Status NVARCHAR(100) NOT NULL,
        AssignedTo NVARCHAR(200) NULL,
        ChangedDate DATETIME2 NOT NULL,
        -- Duration in minutes until next status change (NULL if still active)
        DurationMinutes INT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_DevOpsTaskStatusHistory_Candidates
            FOREIGN KEY (DevOpsTaskCandidateId)
            REFERENCES DevOpsTasksCandidates(Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_DevOpsTaskStatusHistory_CandidateId
        ON DevOpsTaskStatusHistory(DevOpsTaskCandidateId);
END
GO

-- Add DevOps link fields to DevOpsTasksCandidates table
ALTER TABLE DevOpsTasksCandidates ADD DevOpsWorkItemId INT NULL;
GO

ALTER TABLE DevOpsTasksCandidates ADD DevOpsUrl NVARCHAR(500) NULL;
GO

-- Index for quick lookup by DevOps work item ID
CREATE INDEX IX_DevOpsTasksCandidates_DevOpsWorkItemId ON DevOpsTasksCandidates(DevOpsWorkItemId)
WHERE DevOpsWorkItemId IS NOT NULL;
GO

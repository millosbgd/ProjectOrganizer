IF COL_LENGTH('dbo.DevOpsTaskStatusHistory', 'StartedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTaskStatusHistory
        ADD StartedAt datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.DevOpsTaskStatusHistory', 'EndedAt') IS NULL
BEGIN
    ALTER TABLE dbo.DevOpsTaskStatusHistory
        ADD EndedAt datetime2 NULL;
END;
GO

UPDATE dbo.DevOpsTaskStatusHistory
SET StartedAt = ChangedDate
WHERE StartedAt IS NULL;
GO

ALTER TABLE dbo.DevOpsTaskStatusHistory
    ALTER COLUMN StartedAt datetime2 NOT NULL;
GO

UPDATE dbo.DevOpsTaskStatusHistory
SET EndedAt = DATEADD(minute, DurationMinutes, StartedAt)
WHERE EndedAt IS NULL
  AND DurationMinutes IS NOT NULL
  AND DurationMinutes >= 0
  AND StartedAt <= DATEADD(minute, -DurationMinutes, CONVERT(datetime2, '9999-12-31T23:59:59.997'));
GO

DELETE FROM dbo.DevOpsTaskStatusHistory
WHERE StartedAt >= CONVERT(datetime2, '9999-01-01T00:00:00')
   OR ChangedDate >= CONVERT(datetime2, '9999-01-01T00:00:00')
   OR DurationMinutes < 0;
GO

-- =====================================================
-- TEST: UTC DateTime Storage and Retrieval
-- =====================================================
-- This script tests if UTC times are stored correctly
-- Run AFTER the migration to verify everything works
-- =====================================================

USE ProjectOrganizer;
GO

SET NOCOUNT ON;
PRINT '========================================';
PRINT 'UTC DateTime Storage Test';
PRINT 'Current Server Time: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT 'Current UTC Time: ' + CONVERT(VARCHAR, GETUTCDATE(), 120);
PRINT '========================================';
PRINT '';

-- Step 1: Create test activity
DECLARE @TestId INT;
DECLARE @TestUtcStart DATETIME2 = '2026-02-24T15:30:00.0000000'; -- 15:30 UTC
DECLARE @TestUtcEnd DATETIME2 = '2026-02-24T16:30:00.0000000';   -- 16:30 UTC

BEGIN TRANSACTION;

-- Insert test activity
INSERT INTO Aktivnosti (
    Opis, 
    Datum, 
    StartUtc, 
    EndUtc, 
    Status, 
    Vrsta, 
    Bau,
    CreatedAt,
    UpdatedAt
)
VALUES (
    'TEST UTC Storage - DELETE ME',
    '2026-02-24',
    @TestUtcStart,
    @TestUtcEnd,
    'Planirano',
    'Test',
    1, -- BAU activity, no project needed
    GETUTCDATE(),
    GETUTCDATE()
);

SET @TestId = SCOPE_IDENTITY();

PRINT '-- Test record created with ID: ' + CAST(@TestId AS VARCHAR(10));
PRINT '-- Input StartUtc: ' + CONVERT(VARCHAR, @TestUtcStart, 127);
PRINT '-- Input EndUtc: ' + CONVERT(VARCHAR, @TestUtcEnd, 127);
PRINT '';

-- Step 2: Read back and verify
DECLARE @ReadStart DATETIME2;
DECLARE @ReadEnd DATETIME2;

SELECT 
    @ReadStart = StartUtc,
    @ReadEnd = EndUtc
FROM Aktivnosti
WHERE Id = @TestId;

PRINT '-- Stored StartUtc: ' + CONVERT(VARCHAR, @ReadStart, 127);
PRINT '-- Stored EndUtc: ' + CONVERT(VARCHAR, @ReadEnd, 127);
PRINT '';

-- Step 3: Compare values
IF @TestUtcStart = @ReadStart AND @TestUtcEnd = @ReadEnd
BEGIN
    PRINT '✓ SUCCESS: Times stored and retrieved correctly!';
    PRINT '  No timezone conversion occurred.';
END
ELSE
BEGIN
    PRINT '✗ FAILURE: Times do not match!';
    PRINT '  Expected Start: ' + CONVERT(VARCHAR, @TestUtcStart, 127);
    PRINT '  Got Start: ' + CONVERT(VARCHAR, @ReadStart, 127);
    PRINT '  Difference: ' + CAST(DATEDIFF(MINUTE, @TestUtcStart, @ReadStart) AS VARCHAR) + ' minutes';
    PRINT '';
    PRINT '  Expected End: ' + CONVERT(VARCHAR, @TestUtcEnd, 127);
    PRINT '  Got End: ' + CONVERT(VARCHAR, @ReadEnd, 127);
    PRINT '  Difference: ' + CAST(DATEDIFF(MINUTE, @TestUtcEnd, @ReadEnd) AS VARCHAR) + ' minutes';
END
PRINT '';

-- Step 4: Show full record details
PRINT '-- Full test record:';
SELECT 
    Id,
    Opis,
    Datum,
    StartUtc,
    EndUtc,
    DATEDIFF(MINUTE, StartUtc, EndUtc) AS DurationMinutes,
    Status,
    Vrsta
FROM Aktivnosti
WHERE Id = @TestId;
PRINT '';

-- Step 5: Cleanup
DELETE FROM Aktivnosti WHERE Id = @TestId;
PRINT '-- Test record deleted (ID: ' + CAST(@TestId AS VARCHAR(10)) + ')';

ROLLBACK TRANSACTION;
PRINT '-- Transaction rolled back (no permanent changes)';
PRINT '';

-- Step 6: Show existing activities with times
PRINT '-- Sample of existing activities with UTC times:';
SELECT TOP 5
    Id,
    LEFT(Opis, 40) AS Opis,
    Datum,
    StartUtc,
    EndUtc,
    DATEDIFF(MINUTE, StartUtc, EndUtc) AS DurationMinutes
FROM Aktivnosti
WHERE StartUtc IS NOT NULL
ORDER BY StartUtc DESC;

PRINT '';
PRINT '========================================';
PRINT 'Test completed';
PRINT '========================================';
GO

-- =============================================
-- 24_CreateDashboardProcedures.sql
-- Stored Procedures za Dashboard statistiku
-- =============================================

USE ProjectOrganizer;
GO

-- =============================================
-- Procedura: sp_GetDashboardStats
-- Opis: Vraća sve dashboard statistike za korisnika
-- =============================================
IF OBJECT_ID('sp_GetDashboardStats', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetDashboardStats;
GO

CREATE PROCEDURE sp_GetDashboardStats
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. OSNOVNE METRIKE (Kartoni)
    DECLARE @ActiveProjectsCount INT;
    DECLARE @UnfinishedActivitiesCount INT;

    -- Broj aktivnih projekata (kreirao korisnik ili ima permisiju)
    SELECT @ActiveProjectsCount = COUNT(DISTINCT p.Id)
    FROM Projekti p
    WHERE p.Aktivan = 1
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ));

    -- Broj nezavršenih aktivnosti (za projekte korisnika)
    SELECT @UnfinishedActivitiesCount = COUNT(a.Id)
    FROM Aktivnosti a
    INNER JOIN Projekti p ON a.ProjekatId = p.Id
    WHERE a.Status != 'Završeno'
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ));

    -- Rezultat 1: Osnovne metrike
    SELECT 
        @ActiveProjectsCount AS ActiveProjectsCount,
        @UnfinishedActivitiesCount AS UnfinishedActivitiesCount;

    -- Rezultat 2: Projekti po statusu
    SELECT 
        p.Status,
        COUNT(p.Id) AS Count
    FROM Projekti p
    WHERE (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY p.Status
    ORDER BY Count DESC;

    -- Rezultat 3: Aktivnosti po mesecima (poslednjih 6 meseci)
    SELECT 
        FORMAT(a.Datum, 'MMM yyyy', 'sr-Latn-RS') AS Month,
        YEAR(a.Datum) AS Year,
        MONTH(a.Datum) AS MonthNum,
        COUNT(a.Id) AS Count
    FROM Aktivnosti a
    INNER JOIN Projekti p ON a.ProjekatId = p.Id
    WHERE a.Datum >= DATEADD(MONTH, -6, GETDATE())
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY 
        FORMAT(a.Datum, 'MMM yyyy', 'sr-Latn-RS'),
        YEAR(a.Datum),
        MONTH(a.Datum)
    ORDER BY Year, MonthNum;

    -- Rezultat 4: Aktivnosti po statusu (umesto prioriteta koji nemamo)
    SELECT 
        a.Status,
        COUNT(a.Id) AS Count
    FROM Aktivnosti a
    INNER JOIN Projekti p ON a.ProjekatId = p.Id
    WHERE (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY a.Status
    ORDER BY 
        CASE a.Status
            WHEN 'U toku' THEN 1
            WHEN 'Planirano' THEN 2
            WHEN 'Završeno' THEN 3
            ELSE 4
        END;

    -- Rezultat 5: Novi projekti po mesecima (poslednjih 12 meseci)
    SELECT 
        FORMAT(p.Datum, 'MMM yyyy', 'sr-Latn-RS') AS Month,
        YEAR(p.Datum) AS Year,
        MONTH(p.Datum) AS MonthNum,
        COUNT(p.Id) AS Count
    FROM Projekti p
    WHERE p.Datum >= DATEADD(MONTH, -12, GETDATE())
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY 
        FORMAT(p.Datum, 'MMM yyyy', 'sr-Latn-RS'),
        YEAR(p.Datum),
        MONTH(p.Datum)
    ORDER BY Year, MonthNum;

END;
GO

-- =============================================
-- Procedura: sp_GetActiveProjectsCount
-- Opis: Vraća broj aktivnih projekata za korisnika
-- =============================================
IF OBJECT_ID('sp_GetActiveProjectsCount', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetActiveProjectsCount;
GO

CREATE PROCEDURE sp_GetActiveProjectsCount
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(DISTINCT p.Id) AS ActiveProjectsCount
    FROM Projekti p
    WHERE p.Aktivan = 1
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ));
END;
GO

-- =============================================
-- Procedura: sp_GetUnfinishedActivitiesCount
-- Opis: Vraća broj nezavršenih aktivnosti za korisnika
-- =============================================
IF OBJECT_ID('sp_GetUnfinishedActivitiesCount', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetUnfinishedActivitiesCount;
GO

CREATE PROCEDURE sp_GetUnfinishedActivitiesCount
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(a.Id) AS UnfinishedActivitiesCount
    FROM Aktivnosti a
    INNER JOIN Projekti p ON a.ProjekatId = p.Id
    WHERE a.Status != 'Završeno'
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ));
END;
GO

-- =============================================
-- Procedura: sp_GetProjectsByStatus
-- Opis: Vraća raspodelu projekata po statusu
-- =============================================
IF OBJECT_ID('sp_GetProjectsByStatus', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetProjectsByStatus;
GO

CREATE PROCEDURE sp_GetProjectsByStatus
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.Status,
        COUNT(p.Id) AS Count
    FROM Projekti p
    WHERE (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY p.Status
    ORDER BY Count DESC;
END;
GO

-- =============================================
-- Procedura: sp_GetActivitiesByMonth
-- Opis: Vraća broj aktivnosti po mesecima
-- =============================================
IF OBJECT_ID('sp_GetActivitiesByMonth', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetActivitiesByMonth;
GO

CREATE PROCEDURE sp_GetActivitiesByMonth
    @UserId INT,
    @MonthsBack INT = 6
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        FORMAT(a.Datum, 'MMM yyyy', 'sr-Latn-RS') AS Month,
        YEAR(a.Datum) AS Year,
        MONTH(a.Datum) AS MonthNum,
        COUNT(a.Id) AS Count
    FROM Aktivnosti a
    INNER JOIN Projekti p ON a.ProjekatId = p.Id
    WHERE a.Datum >= DATEADD(MONTH, -@MonthsBack, GETDATE())
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY 
        FORMAT(a.Datum, 'MMM yyyy', 'sr-Latn-RS'),
        YEAR(a.Datum),
        MONTH(a.Datum)
    ORDER BY Year, MonthNum;
END;
GO

-- =============================================
-- Procedura: sp_GetActivitiesByStatus
-- Opis: Vraća raspodelu aktivnosti po statusu
-- =============================================
IF OBJECT_ID('sp_GetActivitiesByStatus', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetActivitiesByStatus;
GO

CREATE PROCEDURE sp_GetActivitiesByStatus
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        a.Status,
        COUNT(a.Id) AS Count
    FROM Aktivnosti a
    INNER JOIN Projekti p ON a.ProjekatId = p.Id
    WHERE (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY a.Status
    ORDER BY 
        CASE a.Status
            WHEN 'U toku' THEN 1
            WHEN 'Planirano' THEN 2
            WHEN 'Završeno' THEN 3
            ELSE 4
        END;
END;
GO

-- =============================================
-- Procedura: sp_GetNewProjectsByMonth
-- Opis: Vraća broj novih projekata po mesecima
-- =============================================
IF OBJECT_ID('sp_GetNewProjectsByMonth', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetNewProjectsByMonth;
GO

CREATE PROCEDURE sp_GetNewProjectsByMonth
    @UserId INT,
    @MonthsBack INT = 12
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        FORMAT(p.Datum, 'MMM yyyy', 'sr-Latn-RS') AS Month,
        YEAR(p.Datum) AS Year,
        MONTH(p.Datum) AS MonthNum,
        COUNT(p.Id) AS Count
    FROM Projekti p
    WHERE p.Datum >= DATEADD(MONTH, -@MonthsBack, GETDATE())
      AND (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY 
        FORMAT(p.Datum, 'MMM yyyy', 'sr-Latn-RS'),
        YEAR(p.Datum),
        MONTH(p.Datum)
    ORDER BY Year, MonthNum;
END;
GO

-- =============================================
-- Procedura: sp_GetTopProjectsByActivityCount
-- Opis: Vraća top 5 projekata sa najviše aktivnosti
-- =============================================
IF OBJECT_ID('sp_GetTopProjectsByActivityCount', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetTopProjectsByActivityCount;
GO

CREATE PROCEDURE sp_GetTopProjectsByActivityCount
    @UserId INT,
    @TopCount INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopCount)
        p.Id,
        p.BrojProjekta,
        p.Naziv,
        p.Status,
        COUNT(a.Id) AS ActivityCount
    FROM Projekti p
    LEFT JOIN Aktivnosti a ON p.Id = a.ProjekatId
    WHERE (p.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM ProjectPermissions pp 
               WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
           ))
    GROUP BY p.Id, p.BrojProjekta, p.Naziv, p.Status
    ORDER BY ActivityCount DESC;
END;
GO

-- =============================================
-- Kreiranje indeksa za bolje performanse
-- =============================================

-- Index za pretragu po CreatedBy (ako ne postoji)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Projekti_CreatedBy_Aktivan')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Projekti_CreatedBy_Aktivan
        ON Projekti(CreatedBy, Aktivan)
        INCLUDE (Status, Datum);
END;
GO

-- Index za pretragu aktivnosti po statusu (ako ne postoji)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Aktivnosti_Status_Datum')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Aktivnosti_Status_Datum
        ON Aktivnosti(Status, Datum)
        INCLUDE (ProjekatId);
END;
GO

-- Index za ProjectPermissions (ako ne postoji)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProjectPermissions_UserId_ProjekatId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProjectPermissions_UserId_ProjekatId
        ON ProjectPermissions(UserId, ProjekatId);
END;
GO

PRINT 'Dashboard stored procedures created successfully!';
PRINT '';
PRINT 'Available procedures:';
PRINT '  - sp_GetDashboardStats (@UserId)           - Vraća sve statistike odjednom';
PRINT '  - sp_GetActiveProjectsCount (@UserId)      - Broj aktivnih projekata';
PRINT '  - sp_GetUnfinishedActivitiesCount (@UserId) - Broj nezavršenih aktivnosti';
PRINT '  - sp_GetProjectsByStatus (@UserId)         - Projekti po statusu';
PRINT '  - sp_GetActivitiesByMonth (@UserId, @MonthsBack) - Aktivnosti po mesecima';
PRINT '  - sp_GetActivitiesByStatus (@UserId)       - Aktivnosti po statusu';
PRINT '  - sp_GetNewProjectsByMonth (@UserId, @MonthsBack) - Novi projekti po mesecima';
PRINT '  - sp_GetTopProjectsByActivityCount (@UserId, @TopCount) - Top projekti';
PRINT '';
PRINT 'Example usage:';
PRINT '  EXEC sp_GetDashboardStats @UserId = 1;';
GO

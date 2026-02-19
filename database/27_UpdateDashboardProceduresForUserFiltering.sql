-- =============================================
-- 27_UpdateDashboardProceduresForUserFiltering.sql
-- Ažuriranje stored procedura za filtriranje aktivnosti po korisniku
-- =============================================

USE ProjectOrganizer;
GO

PRINT 'Ažuriranje stored procedura za dashboard filtriranje...';
GO

-- =============================================
-- Procedura: sp_GetDashboardStats
-- Opis: Vraća sve dashboard statistike za korisnika
-- Ažurirano: Filtrira aktivnosti po korisniku koji ih je kreirao
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

    -- Broj nezavršenih aktivnosti (samo one koje je korisnik kreirao)
    SELECT @UnfinishedActivitiesCount = COUNT(a.Id)
    FROM Aktivnosti a
    WHERE a.Status != 'Završeno'
      AND a.CreatedBy = @UserId;

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
    WHERE a.Datum >= DATEADD(MONTH, -6, GETDATE())
      AND a.CreatedBy = @UserId
    GROUP BY 
        FORMAT(a.Datum, 'MMM yyyy', 'sr-Latn-RS'),
        YEAR(a.Datum),
        MONTH(a.Datum)
    ORDER BY Year, MonthNum;

    -- Rezultat 4: Aktivnosti po statusu
    SELECT 
        a.Status,
        COUNT(a.Id) AS Count
    FROM Aktivnosti a
    WHERE a.CreatedBy = @UserId
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
-- Procedura: sp_GetUnfinishedActivitiesCount
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
    WHERE a.Status != 'Završeno'
      AND a.CreatedBy = @UserId;
END;
GO

-- =============================================
-- Procedura: sp_GetActivitiesByMonth
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
    WHERE a.Datum >= DATEADD(MONTH, -@MonthsBack, GETDATE())
      AND a.CreatedBy = @UserId
    GROUP BY 
        FORMAT(a.Datum, 'MMM yyyy', 'sr-Latn-RS'),
        YEAR(a.Datum),
        MONTH(a.Datum)
    ORDER BY Year, MonthNum;
END;
GO

-- =============================================
-- Procedura: sp_GetActivitiesByStatus
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
    WHERE a.CreatedBy = @UserId
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
-- Procedura: sp_GetTopProjectsByActivityCount
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
    LEFT JOIN Aktivnosti a ON p.Id = a.ProjekatId AND a.CreatedBy = @UserId
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
-- Kreiranje/Ažuriranje indeksa za bolje performanse
-- =============================================

-- Ažuriranje postojećeg indeksa da uključi CreatedBy
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Aktivnosti_Status_Datum')
BEGIN
    DROP INDEX IX_Aktivnosti_Status_Datum ON Aktivnosti;
END;
GO

CREATE NONCLUSTERED INDEX IX_Aktivnosti_Status_Datum
    ON Aktivnosti(Status, Datum)
    INCLUDE (ProjekatId, CreatedBy);
GO

-- Novi indeks za pretragu po CreatedBy
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Aktivnosti_CreatedBy')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Aktivnosti_CreatedBy
        ON Aktivnosti(CreatedBy)
        INCLUDE (Status, Datum, ProjekatId);
END;
GO

PRINT 'Dashboard stored procedures successfully updated!';
PRINT 'Aktivnosti se sada filtriraju po korisniku koji ih je kreirao.';
GO

-- =============================================
-- 28_PopulateCreatedByForActivities.sql
-- Popunjavanje CreatedBy polja za postojeće aktivnosti
-- =============================================

USE ProjectOrganizer;
GO

PRINT 'Popunjavanje CreatedBy za postojeće aktivnosti...';
GO

-- =============================================
-- Popuni CreatedBy za aktivnosti koje imaju projekat
-- Postavi CreatedBy na osnovu kreatora projekta
-- =============================================

UPDATE a
SET a.CreatedBy = p.CreatedBy
FROM Aktivnosti a
INNER JOIN Projekti p ON a.ProjekatId = p.Id
WHERE a.CreatedBy IS NULL
  AND p.CreatedBy IS NOT NULL;

PRINT CONCAT('Ažurirano aktivnosti sa projektom: ', @@ROWCOUNT, ' redova');
GO

-- =============================================
-- Za BAU aktivnosti (bez projekta), postavi na prvog admina
-- ili na default korisnika
-- =============================================

DECLARE @DefaultUserId INT;

-- Pokušaj da pronađeš prvog admin korisnika
SELECT TOP 1 @DefaultUserId = Id 
FROM Users 
WHERE Role = 'Admin'
ORDER BY Id;

-- Ako ne postoji admin, uzmi prvog korisnika
IF @DefaultUserId IS NULL
BEGIN
    SELECT TOP 1 @DefaultUserId = Id 
    FROM Users 
    ORDER BY Id;
END;

-- Ažuriraj BAU aktivnosti
UPDATE Aktivnosti
SET CreatedBy = @DefaultUserId
WHERE CreatedBy IS NULL
  AND ProjekatId IS NULL;

PRINT CONCAT('Ažurirano BAU aktivnosti: ', @@ROWCOUNT, ' redova');
PRINT CONCAT('Korišćen UserId: ', @DefaultUserId);
GO

-- =============================================
-- Proveri rezultate
-- =============================================

SELECT 
    COUNT(*) as TotalActivities,
    COUNT(CreatedBy) as WithCreatedBy,
    COUNT(*) - COUNT(CreatedBy) as WithoutCreatedBy
FROM Aktivnosti;
GO

SELECT 
    CASE 
        WHEN a.ProjekatId IS NULL THEN 'BAU'
        ELSE 'Sa projektom'
    END as TipAktivnosti,
    COUNT(*) as Total,
    COUNT(a.CreatedBy) as SaCreatedBy,
    COUNT(*) - COUNT(a.CreatedBy) as BezCreatedBy
FROM Aktivnosti a
GROUP BY 
    CASE 
        WHEN a.ProjekatId IS NULL THEN 'BAU'
        ELSE 'Sa projektom'
    END;
GO

PRINT '';
PRINT 'CreatedBy polje uspešno popunjeno za sve postojeće aktivnosti!';
GO

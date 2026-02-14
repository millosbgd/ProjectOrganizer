-- 19_PopulateCreatedBy.sql
-- Popunjavanje CreatedBy kolone za postojeće projekte na osnovu ProjectPermissions

-- Za projekte koji već imaju permissions, postavljamo CreatedBy na prvog admina ili prvog korisnika
UPDATE p
SET p.CreatedBy = (
    SELECT TOP 1 pp.UserId
    FROM ProjectPermissions pp
    WHERE pp.ProjekatId = p.Id
    ORDER BY 
        CASE WHEN pp.PermissionLevel = 'Admin' THEN 0 ELSE 1 END,
        pp.Id
)
FROM Projekti p
WHERE p.CreatedBy IS NULL
AND EXISTS (
    SELECT 1 
    FROM ProjectPermissions pp 
    WHERE pp.ProjekatId = p.Id
);

-- Za projekte bez permissions, postavljamo na prvog admin korisnika u sistemu
UPDATE p
SET p.CreatedBy = (
    SELECT TOP 1 Id 
    FROM Users 
    WHERE Role = 'Admin'
    ORDER BY Id
)
FROM Projekti p
WHERE p.CreatedBy IS NULL;

PRINT 'CreatedBy values populated for existing projects!';

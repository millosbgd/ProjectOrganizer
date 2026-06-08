INSERT INTO dbo.UserMenuPermissions (UserId, MenuKey)
SELECT u.Id, 'posete-gorivo'
FROM dbo.Users u
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.UserMenuPermissions p
    WHERE p.UserId = u.Id
      AND p.MenuKey = 'posete-gorivo'
);

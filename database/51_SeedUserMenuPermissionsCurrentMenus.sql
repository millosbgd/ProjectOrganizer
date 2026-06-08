INSERT INTO dbo.UserMenuPermissions (UserId, MenuKey)
SELECT u.Id, m.MenuKey
FROM dbo.Users u
CROSS JOIN (VALUES
    ('dashboard'),
    ('projekti'),
    ('klijenti'),
    ('aktivnosti'),
    ('kalendar'),
    ('implementation-models'),
    ('admin-users'),
    ('settings')
) AS m(MenuKey)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.UserMenuPermissions p
    WHERE p.UserId = u.Id
      AND p.MenuKey = m.MenuKey
);

-- Rollback za Šablone za podršku
-- Briše menu permission i tabelu sa podacima.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.UserMenuPermissions', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.UserMenuPermissions
    WHERE MenuKey = N'sabloni-podrske';
END;

IF OBJECT_ID(N'dbo.SabloniPodrske', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.SabloniPodrske;
END;

COMMIT TRANSACTION;

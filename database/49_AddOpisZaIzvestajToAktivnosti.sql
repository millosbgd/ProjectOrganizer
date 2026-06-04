IF COL_LENGTH('dbo.Aktivnosti', 'OpisZaIzvestaj') IS NULL
BEGIN
    ALTER TABLE dbo.Aktivnosti
        ADD OpisZaIzvestaj nvarchar(4000) NOT NULL
            CONSTRAINT DF_Aktivnosti_OpisZaIzvestaj DEFAULT ('');
END
GO

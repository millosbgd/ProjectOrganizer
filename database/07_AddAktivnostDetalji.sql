-- Dodavanje kolone Detalji za aktivnosti
ALTER TABLE Aktivnosti
ADD Detalji NVARCHAR(2000) NOT NULL CONSTRAINT DF_Aktivnosti_Detalji DEFAULT ('');

GO

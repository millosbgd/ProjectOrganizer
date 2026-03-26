-- Migration 36: Create DailyTasks table
-- Dnevni taskovi korisnika

CREATE TABLE DailyTasks (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    Datum             DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Opis              NVARCHAR(500) NOT NULL,
    KorisnikKreirao   INT           NOT NULL,
    Solved            BIT           NOT NULL DEFAULT 0,
    Pinned            BIT           NOT NULL DEFAULT 0,

    CONSTRAINT FK_DailyTasks_Users FOREIGN KEY (KorisnikKreirao) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_DailyTasks_KorisnikKreirao       ON DailyTasks(KorisnikKreirao);
CREATE INDEX IX_DailyTasks_KorisnikKreirao_Datum  ON DailyTasks(KorisnikKreirao, Datum);

-- Migration 33: Create AiReminders table
-- GPT ekstrahuje podsetnike iz teksta aktivnosti pri čuvanju (scan-on-write).
-- Background service proverava tabelu svaki sat i šalje notifikaciju kad RemindAt istekne.

CREATE TABLE AiReminders (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    AktivnostId INT NOT NULL,
    ProjekatId  INT NULL,
    UserId      INT NOT NULL,
    RemindAt    DATETIME2 NOT NULL,
    Message     NVARCHAR(500) NOT NULL,
    Sent        BIT NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_AiReminders_Aktivnosti FOREIGN KEY (AktivnostId)
        REFERENCES Aktivnosti(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AiReminders_Projekti FOREIGN KEY (ProjekatId)
        REFERENCES Projekti(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_AiReminders_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_AiReminders_RemindAt_Sent ON AiReminders (RemindAt, Sent);
CREATE INDEX IX_AiReminders_AktivnostId   ON AiReminders (AktivnostId);

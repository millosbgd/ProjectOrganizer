-- Migration 31: Create Notifications table
-- Sistem notifikacija za projekte i remindere

CREATE TABLE Notifications (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL,
    ProjekatId  INT NULL,
    Type        NVARCHAR(50)  NOT NULL,
    Message     NVARCHAR(500) NOT NULL,
    IsRead      BIT           NOT NULL DEFAULT 0,
    -- Koristi se za sprečavanje duplikata za isti događaj
    -- Format: "{Type}:{ProjekatId}:{yyyy-MM-dd}"
    ReferenceKey NVARCHAR(200) NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_Notifications_Users    FOREIGN KEY (UserId)     REFERENCES Users(Id)    ON DELETE CASCADE,
    CONSTRAINT FK_Notifications_Projekti FOREIGN KEY (ProjekatId) REFERENCES Projekti(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Notifications_UserId         ON Notifications(UserId);
CREATE INDEX IX_Notifications_UserId_IsRead  ON Notifications(UserId, IsRead);
CREATE INDEX IX_Notifications_ReferenceKey   ON Notifications(ReferenceKey) WHERE ReferenceKey IS NOT NULL;

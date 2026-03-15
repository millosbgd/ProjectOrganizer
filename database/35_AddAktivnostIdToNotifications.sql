-- Dodaje kolonu AktivnostId na tabelu Notifications
-- Opciona FK ka Aktivnosti – koristi se za link u notifikacionom panelu

ALTER TABLE Notifications
ADD AktivnostId INT NULL;

ALTER TABLE Notifications
ADD CONSTRAINT FK_Notifications_Aktivnosti
    FOREIGN KEY (AktivnostId) REFERENCES Aktivnosti(Id) ON DELETE NO ACTION;

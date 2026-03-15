-- Dodaje kolonu Dismissed na tabelu Notifications
-- Default: false (prikazuje se normalno)
-- Kad korisnik klikne X, setuje se na true i notifikacija se više ne prikazuje

ALTER TABLE Notifications
ADD Dismissed BIT NOT NULL DEFAULT 0;

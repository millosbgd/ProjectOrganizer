-- Migration 32: Add AIPracen to Projekti
-- Označava da li AI prati projekat (background reminder notifikacije)

ALTER TABLE Projekti
    ADD AIPracen BIT NOT NULL DEFAULT 0;

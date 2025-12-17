USE ProjectOrganizer;
GO

-- Seed Klijenti
IF NOT EXISTS (SELECT * FROM Klijenti)
BEGIN
    INSERT INTO Klijenti (Naziv, Adresa, Grad, Zemlja) VALUES
    ('Kompanija A', 'Kneza Miloša 10', 'Beograd', 'Srbija'),
    ('Kompanija B', 'Bulevar kralja Aleksandra 50', 'Beograd', 'Srbija'),
    ('Tech Solutions LLC', '123 Main Street', 'New York', 'USA');
END
GO

-- Seed Projekti
IF NOT EXISTS (SELECT * FROM Projekti)
BEGIN
    DECLARE @KlijentId1 INT = (SELECT TOP 1 Id FROM Klijenti WHERE Naziv = 'Kompanija A');
    DECLARE @KlijentId2 INT = (SELECT TOP 1 Id FROM Klijenti WHERE Naziv = 'Kompanija B');
    
    INSERT INTO Projekti (BrojProjekta, Datum, Naziv, Aktivan, Status, KlijentId) VALUES
    ('PRJ-2025-001', '2025-01-15', 'Web aplikacija za evidenciju', 1, 'U toku', @KlijentId1),
    ('PRJ-2025-002', '2025-02-01', 'Mobilna aplikacija', 1, 'Planiranje', @KlijentId2),
    ('PRJ-2024-050', '2024-11-10', 'CRM sistem', 0, 'Završeno', @KlijentId1);
END
GO

-- Seed Aktivnosti
IF NOT EXISTS (SELECT * FROM Aktivnosti)
BEGIN
    DECLARE @ProjekatId1 INT = (SELECT TOP 1 Id FROM Projekti WHERE BrojProjekta = 'PRJ-2025-001');
    DECLARE @ProjekatId2 INT = (SELECT TOP 1 Id FROM Projekti WHERE BrojProjekta = 'PRJ-2025-002');
    
    INSERT INTO Aktivnosti (Opis, Datum, Status, Vrsta, ProjekatId) VALUES
    ('Analiza zahteva', '2025-01-15', 'Završeno', 'Analiza', @ProjekatId1),
    ('Dizajn baze podataka', '2025-01-20', 'Završeno', 'Dizajn', @ProjekatId1),
    ('Implementacija backend-a', '2025-02-01', 'U toku', 'Razvoj', @ProjekatId1),
    ('Testiranje sistema', '2025-02-15', 'Planirano', 'Testiranje', @ProjekatId1),
    ('Inicijalni sastanak', '2025-02-01', 'Završeno', 'Sastanak', @ProjekatId2),
    ('Priprema wireframe-a', '2025-02-05', 'U toku', 'Dizajn', @ProjekatId2);
END
GO

# Database Setup

## SQL Server konfiguracija

- **Server**: MILOS-LAPTOP
- **Database**: ProjectOrganizer
- **User**: sa
- **Password**: sql

## Pokretanje SQL skripti

Izvršiti skripte sledećim redom:

1. `01_CreateDatabase.sql` - Kreira bazu podataka
2. `02_CreateTables.sql` - Kreira tabele i indekse
3. `03_SeedData.sql` - Unosi test podatke

### SQL Server Management Studio (SSMS)

```sql
-- Otvori svaku skriptu u SSMS i izvrši
```

### sqlcmd (Command Line)

```powershell
sqlcmd -S MILOS-LAPTOP -U sa -P sql -i 01_CreateDatabase.sql
sqlcmd -S MILOS-LAPTOP -U sa -P sql -i 02_CreateTables.sql
sqlcmd -S MILOS-LAPTOP -U sa -P sql -i 03_SeedData.sql
```

## Šema baze

### Tabela: Klijenti
- Id (PK, Identity)
- Naziv
- Adresa
- Grad
- Zemlja
- CreatedAt, UpdatedAt

### Tabela: Projekti
- Id (PK, Identity)
- BrojProjekta (Unique)
- Datum
- Naziv
- Aktivan (bit)
- Status
- KlijentId (FK -> Klijenti)
- CreatedAt, UpdatedAt

### Tabela: Aktivnosti
- Id (PK, Identity)
- Opis
- Datum
- Status
- Vrsta
- ProjekatId (FK -> Projekti)
- CreatedAt, UpdatedAt

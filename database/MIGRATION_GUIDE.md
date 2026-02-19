# Database Migracije za Filtriranje Aktivnosti

## Problem
Filtriranje aktivnosti po korisniku ne radi jer:
1. Stored procedure za Dashboard nisu ažurirane na bazi
2. Neke aktivnosti nemaju `CreatedBy` polje popunjeno

## Rešenje

Pokreni sledeće SQL skripte na bazi **REDOM**:

### 1. Popuni CreatedBy za postojeće aktivnosti
```sql
-- Pokreni: database/28_PopulateCreatedByForActivities.sql
```
Ova skripta:
- Popunjava `CreatedBy` za aktivnosti koje imaju projekat (na osnovu kreatora projekta)
- Popunjava `CreatedBy` za BAU aktivnosti (postavlja na prvog admin korisnika)
- Prikazuje statistiku pre i posle

### 2. Ažuriraj Dashboard Stored Procedures
```sql
-- Pokreni: database/27_UpdateDashboardProceduresForUserFiltering.sql
```
Ova skripta:
- Ažurira `sp_GetDashboardStats` da filtrira aktivnosti po `CreatedBy`
- Ažurira ostale stored procedure za dashboard
- Kreira indekse za bolje performanse

## Kako pokrenuti

### Opcija 1: SQL Server Management Studio (SSMS)
1. Otvori SSMS
2. Konektuj se na server
3. Otvori `28_PopulateCreatedByForActivities.sql`
4. Klikni Execute (F5)
5. Otvori `27_UpdateDashboardProceduresForUserFiltering.sql`
6. Klikni Execute (F5)

### Opcija 2: sqlcmd (Komandna linija)
```bash
# Lokalna baza
sqlcmd -S localhost -d ProjectOrganizer -E -i database/28_PopulateCreatedByForActivities.sql
sqlcmd -S localhost -d ProjectOrganizer -E -i database/27_UpdateDashboardProceduresForUserFiltering.sql

# Azure SQL Database
sqlcmd -S <server>.database.windows.net -d ProjectOrganizer -U <username> -P <password> -i database/28_PopulateCreatedByForActivities.sql
sqlcmd -S <server>.database.windows.net -d ProjectOrganizer -U <username> -P <password> -i database/27_UpdateDashboardProceduresForUserFiltering.sql
```

### Opcija 3: Azure Data Studio
1. Otvori Azure Data Studio
2. Konektuj se na bazu
3. Otvori New Query
4. Kopiraj sadržaj `28_PopulateCreatedByForActivities.sql`
5. Execute
6. Ponovi za `27_UpdateDashboardProceduresForUserFiltering.sql`

## Verifikacija

Nakon pokretanja skripti:
1. **Proveri aktivnosti**:
   ```sql
   SELECT COUNT(*) as Total, COUNT(CreatedBy) as WithCreatedBy 
   FROM Aktivnosti;
   ```
   Trebalo bi da `Total = WithCreatedBy`

2. **Proveri stored procedures**:
   ```sql
   EXEC sp_GetDashboardStats @UserId = 1;
   ```
   Trebalo bi da vrati samo aktivnosti korisnika sa ID=1

3. **Proveri indekse**:
   ```sql
   SELECT name FROM sys.indexes 
   WHERE object_id = OBJECT_ID('Aktivnosti') 
   AND name IN ('IX_Aktivnosti_CreatedBy', 'IX_Aktivnosti_Status_Datum');
   ```
   Trebalo bi da vrati 2 reda

## Napomena
⚠️ **VAŽNO**: Pokreni ove skripte i na **lokalnoj** i na **production** bazi!

## Šta nakon migracije?
Nakon uspešnog pokretanja migracija:
- Dashboard će prikazivati samo aktivnosti trenutno ulogovanog korisnika
- Lista aktivnosti će defaultno prikazivati samo aktivnosti korisnika
- Dugme "Prikaži sve aktivnosti" će prikazati SVE aktivnosti

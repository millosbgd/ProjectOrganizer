# Deployment Workflow - ProjectOrganizer

Brzi vodič za deploy frontend-a i backend-a.

---

## 📦 Frontend Deployment (GitHub → Azure Static Web Apps)

### Automatski deployment kroz GitHub Actions

Frontend se **automatski** deploy-uje na Azure Static Web Apps kada push-uješ na `deploy` branch.

### Koraci:

1. **Commituj sve promene:**
   ```powershell
   git add .
   git commit -m "Opis promene"
   ```

2. **Push na deploy branch:**
   ```powershell
   git push origin deploy
   ```

3. **Proveri GitHub Actions:**
   - Idi na: https://github.com/millosbgd/ProjectOrganizer/actions
   - Prati progress build-a
   - Kada je gotovo, frontend je live na: https://jolly-ocean-0615ca003.3.azurestaticapps.net

4. **Hard refresh browsera:**
   - `Ctrl + Shift + R` (Windows/Linux)
   - `Cmd + Shift + R` (Mac)

**Napomena:** GitHub Actions automatski:
- Build-uje Angular aplikaciju
- Deploy-uje na Azure Static Web Apps
- Sve se dešava kroz `.github/workflows/azure-static-web-apps.yml`

---

## 🚀 Backend Deployment (Manual → Azure App Service)

Backend se deploy-uje **ručno** kroz Azure CLI.

### Preduslovi:
- Azure CLI instaliran
- Ulogovan na Azure: `az login`

### Način 1: Automatski (Preporučeno) 🎯

Koristi PowerShell skriptu:

```powershell
# Iz root direktorijuma projekta
.\deploy-backend.ps1
```

Skripta automatski:
- ✓ Proverava Azure CLI i login
- ✓ Build-uje backend u Release mode
- ✓ Kreira ZIP arhivu
- ✓ Deploy-uje na Azure
- ✓ Čisti privremene fajlove
- ✓ Prikazuje deployment status

### Način 2: Manualno

1. **Build backend-a u Release mode:**
   ```powershell
   cd backend/ProjectOrganizer.Api
   dotnet publish -c Release -o ./publish
   ```

2. **Kreiraj ZIP arhivu:**
   ```powershell
   cd publish
   Compress-Archive -Path * -DestinationPath ../app.zip -Force
   cd ..
   ```

3. **Deploy na Azure App Service:**
   ```powershell
   az webapp deployment source config-zip `
     --resource-group rg-projectorganizer-dev `
     --name ProjectOrganizer `
     --src app.zip
   ```

4. **Proveri status:**
   Komanda će pokazati:
   ```
   Status: Building the app... Time: X(s)
   Status: Build successful. Time: Y(s)
   ```

5. **Backend je live:**
   - URL: https://projectorganizer.azurewebsites.net
   - API: https://projectorganizer.azurewebsites.net/api

### Database Migration (ako ima novih SQL skripti):

**Ako imaš nove SQL skripte** (npr. `17_AddCalendarColumnsToAktivnosti.sql`):

1. **Konektuj se na Azure SQL:**
   - Koristi SQL Server Management Studio (SSMS)
   - Server: `projectorganizer-sql.database.windows.net`
   - Database: `ProjectOrganizer`
   - Authentication: SQL Server Authentication
   - Username/Password: (iz Azure Portal-a)

2. **Izvršavaj SQL skripte redom:**
   ```sql
   -- Otvori skriptu u SSMS i izvrši (F5)
   ```

---

## ⚡ Quick Deploy - UTC DateTime Fix (29_AlterCalendarColumnsToDateTime2)

**Ovaj deployment uključuje:**
- ✅ Backend kod izmene (UtcDateTimeConverter.cs, ApplicationDbContext.cs)
- ✅ SQL migraciju (29_AlterCalendarColumnsToDateTime2.sql)
- ℹ️ Bez frontend izmena

### Koraci (izvršavati REDOM):

#### 1. Commituj izmene
```powershell
git add .
git commit -m "Fix UTC DateTime timezone issues for aktivnosti"
git push origin deploy
```

#### 2. SQL Migracija (PRVO!)
```powershell
# Otvori SSMS i konektuj se na Azure SQL:
# Server: projectorganizer-sql.database.windows.net
# Database: ProjectOrganizer
# Auth: SQL Server Authentication

# Otvori i izvrši (F5):
# database/29_AlterCalendarColumnsToDateTime2.sql

# (Opciono) Testiraj:
# database/TEST_UTC_DateTime.sql
```

#### 3. Backend Deploy (ZATIM!)
```powershell
# Automatski način (preporučeno):
.\deploy-backend.ps1

# ILI manualno:
cd backend/ProjectOrganizer.Api
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../app.zip -Force
cd ..
az webapp deployment source config-zip --resource-group rg-projectorganizer-dev --name ProjectOrganizer --src app.zip
```

#### 4. Verifikacija
```powershell
# Testiraj API:
# https://projectorganizer.azurewebsites.net/api/aktivnosti

# Proveri da li se vremena čuvaju ispravno (bez timezone konverzije)
```

**⚠️ KRITIČNO:** SQL migracija **MORA** biti izvršena **PRE** backend deploya!
- Backend očekuje DATETIME2 kolone i UTC converter
- Ako deploy-uješ backend pre SQL migracije → greška!

---

## 🔄 Kompletni Deployment Proces

Kada radiš nove feature-e:

### DevOps dnevni sync taskova (47_AddDevOpsSyncMetadata)

**Ovaj deployment uključuje:**
- ✅ Backend dnevni background sync u 07:00
- ✅ Osvežavanje DevOps taskova PAT tokenom korisnika koji ih je kreirao
- ✅ Nova metadata polja u `DevOpsTasksCandidates`
- ✅ Minimalna notifikacija posle sync-a

### Koraci:

1. **SQL migracija (već izvršena ako si pustio skriptu):**
   ```sql
   -- database/47_AddDevOpsSyncMetadata.sql
   ```

2. **Backend deploy:**
   ```powershell
   .\deploy-backend.ps1
   ```

3. **Commit/push na deploy branch:**
   ```powershell
   git add .
   git commit -m "Add daily DevOps task sync"
   git push origin deploy
   ```

4. **Verifikacija:**
   - Proveri App Service logs posle deploy-a
   - Background servis treba da loguje sledeće zakazano DevOps osvežavanje
   - Sync će se automatski izvršiti sledećeg dana u 07:00

**Napomena:** SQL migracija mora biti izvršena pre backend deploy-a, jer backend očekuje nova DevOps sync polja.

### 1. Frontend + Backend promene:

```powershell
# 1. Commituj sve
git add .
git commit -m "Add new feature"

# 2. Push frontend (automatski deploy)
git push origin deploy

# 3. Build i deploy backend
cd backend/ProjectOrganizer.Api
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../app.zip -Force
cd ..
az webapp deployment source config-zip --resource-group rg-projectorganizer-dev --name ProjectOrganizer --src app.zip

# 4. Ako ima database promene - izvršiti SQL skripta u SSMS na Azure SQL
```

### 2. Samo Frontend promene:

```powershell
git add .
git commit -m "Frontend update"
git push origin deploy
# Gotovo! GitHub Actions će automatski deploy-ovati
```

### 3. Samo Backend promene:

```powershell
# 1. Commituj za verzionisanje
git add .
git commit -m "Backend update"
git push origin deploy

# 2. Deploy backend
cd backend/ProjectOrganizer.Api
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../app.zip -Force
cd ..
az webapp deployment source config-zip --resource-group rg-projectorganizer-dev --name ProjectOrganizer --src app.zip
```

---

## 🐛 Troubleshooting

### Frontend ne radi nakon deploya:
- Hard refresh: `Ctrl + Shift + R`
- Proveri GitHub Actions status
- Vidi da li ima build grešaka u Actions tab-u

### Backend ne radi:
- Proveri Azure Portal → App Service → Logs
- Proveri da li su environment variables postavljene (Auth0, ConnectionString)
- Testaj API direktno: `https://projectorganizer.azurewebsites.net/api/projekti`

### Database promene nisu vidljive:
- Proveri da li je SQL skripta izvršena na **Azure SQL**, ne na lokalnoj bazi
- Proveri Connection String u App Service settings

---

## 📝 Azure Resources

- **Resource Group:** `rg-projectorganizer-dev`
- **Frontend (Static Web App):** `projectorganizer-frontend`
  - URL: https://jolly-ocean-0615ca003.3.azurestaticapps.net
- **Backend (App Service):** `ProjectOrganizer`
  - URL: https://projectorganizer.azurewebsites.net
- **Database (Azure SQL):** `projectorganizer-sql.database.windows.net`
  - Database Name: `ProjectOrganizer`

---

## ✅ Checklist Pre-Deployment

**Generalno:**
- [ ] Sve promene commitovane
- [ ] Testirao lokalno (ako moguće)
- [ ] SQL skripte pripremljene (ako ima database promena)
- [ ] Backend build-a bez error-a
- [ ] Frontend build-a bez error-a

**Deployment redosled (KRITIČNO):**
1. [ ] SQL migracije izvršene na Azure SQL **PRVO**
2. [ ] Backend deploy (ako ima backend izmena)
3. [ ] Frontend deploy/push (ako ima frontend izmena)
4. [ ] Hard refresh browsera
5. [ ] Testirao na production URL-u

**Zašto ovaj redosled?**
- Backend kod može zahtevati nove kolone/tabele iz SQL migracija
- Frontend može zahtevati nove API endpoint-e iz backend-a
- Uvek: **Database → Backend → Frontend**

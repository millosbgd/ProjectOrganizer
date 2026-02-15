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
   - Kada je gotovo, frontend je live na: https://delightful-pebble-0fc0d9403.4.azurestaticapps.net

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

### Koraci:

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

## 🔄 Kompletni Deployment Proces

Kada radiš nove feature-e:

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
  - URL: https://delightful-pebble-0fc0d9403.4.azurestaticapps.net
- **Backend (App Service):** `ProjectOrganizer`
  - URL: https://projectorganizer.azurewebsites.net
- **Database (Azure SQL):** `projectorganizer-sql.database.windows.net`
  - Database Name: `ProjectOrganizer`

---

## ✅ Checklist Pre-Deployment

- [ ] Sve promene commitovane
- [ ] Testirao lokalno (ako moguće)
- [ ] SQL skripte pripremljene (ako ima database promena)
- [ ] Backend build-a bez error-a
- [ ] Frontend build-a bez error-a
- [ ] Push na deploy branch
- [ ] Backend deploy kroz Azure CLI
- [ ] SQL skripte izvršene na Azure SQL
- [ ] Hard refresh browsera
- [ ] Testirao na production URL-u

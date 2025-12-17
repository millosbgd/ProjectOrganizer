# Azure Deployment - Portal Vodič

Koraci za deployment preko Azure Portal-a (mnogo lakše od CLI-ja!)

---

## 🔐 Korak 1: Prijavi se

Idi na: **https://portal.azure.com**

---

## 📦 Korak 2: Kreiraj Resource Group

1. Klikni **"Create a resource"** ili **"Resource groups"**
2. Klikni **"+ Create"**
3. Popuni:
   - **Subscription:** Tvoja subscription
   - **Resource group name:** `ProjectOrganizerRG`
   - **Region:** `West Europe` ili bilo koja blizu tebe
4. Klikni **"Review + Create"** → **"Create"**

---

## 🗄️ Korak 3: Kreiraj Azure SQL Database

### 3.1 Kreiraj SQL Server i Bazu

1. Klikni **"Create a resource"**
2. Pretraži: `SQL Database`
3. Klikni **"Create"**
4. Popuni:
   
   **Basics tab:**
   - **Resource group:** `ProjectOrganizerRG`
   - **Database name:** `ProjectOrganizer`
   - **Server:** Klikni **"Create new"**
     - **Server name:** `projectorganizer-sql-server` (mora biti globalno jedinstveno)
     - **Location:** `West Europe`
     - **Authentication method:** Use SQL authentication
     - **Server admin login:** `sqladmin`
     - **Password:** Unesi jaku lozinku (sačuvaj je!)
     - Klikni **"OK"**
   - **Want to use SQL elastic pool?** No
   - **Compute + storage:** Klikni **"Configure database"**
     - Izaberi **"Basic"** (najjeftinije, $5/mesec)
     - Klikni **"Apply"**

   **Networking tab:**
   - **Connectivity method:** Public endpoint
   - **Allow Azure services...:** YES ✅
   - **Add current client IP address:** YES ✅

5. Klikni **"Review + Create"** → **"Create"**

⏱️ Čekaj 2-3 minuta...

### 3.2 Popuni bazu podacima

1. Kada je kreirana, idi u SQL Database → **"Query editor"**
2. Loguj se sa:
   - **SQL server authentication**
   - Login: `sqladmin`
   - Password: [tvoja lozinka]
3. Otvori fajl `database/02_CreateTables.sql` i kopiraj ceo sadržaj
4. Nalepi u Query editor i klikni **"Run"**
5. Isto uradi za `database/03_SeedData.sql`

✅ Baza je spremna!

---

## ⚙️ Korak 4: Backend - App Service

### 4.1 Kreiraj App Service

1. Klikni **"Create a resource"**
2. Pretraži: `Web App`
3. Klikni **"Create"**
4. Popuni:

   **Basics tab:**
   - **Resource Group:** `ProjectOrganizerRG`
   - **Name:** `projectorganizer-api` (mora biti globalno jedinstveno)
   - **Publish:** Code
   - **Runtime stack:** .NET 8 (LTS)
   - **Operating System:** Linux
   - **Region:** West Europe
   - **Pricing plan:** Klikni **"Create new"**
     - Name: `ProjectOrganizerPlan`
     - Size: **B1** (Basic, ~$13/mesec) ili **F1** (Free - za testiranje)

   **Monitoring tab:**
   - **Enable Application Insights:** Yes (opciono)

5. Klikni **"Review + Create"** → **"Create"**

⏱️ Čekaj 1-2 minuta...

### 4.2 Konfiguriši Connection String

1. Idi u App Service → **"Configuration"** (levi meni)
2. U **"Connection strings"** klikni **"+ New connection string"**
3. Popuni:
   - **Name:** `DefaultConnection`
   - **Value:** 
     ```
     Server=tcp:projectorganizer-sql-server.database.windows.net,1433;Initial Catalog=ProjectOrganizer;Persist Security Info=False;User ID=sqladmin;Password=[TVOJA_LOZINKA];MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
     ```
     ⚠️ **Zameni `[TVOJA_LOZINKA]` i ime servera ako je drugačije!**
   - **Type:** SQLAzure
4. Klikni **"OK"**

### 4.3 Dodaj Auth0 Settings

1. U istom **"Configuration"** → **"Application settings"**
2. Klikni **"+ New application setting"** i dodaj:
   - **Name:** `Auth0__Domain`
   - **Value:** `[TVOJ_AUTH0_DOMAIN]` (npr. `dev-xxx.us.auth0.com`)
3. Klikni **"+ New application setting"** ponovo:
   - **Name:** `Auth0__Audience`
   - **Value:** `[TVOJ_AUTH0_API_IDENTIFIER]`
4. Klikni **"Save"** na vrhu

### 4.4 Konfiguriši CORS

1. Idi u App Service → **"CORS"** (levi meni)
2. U **"Allowed Origins"** dodaj:
   - `http://localhost:4200` (za local testiranje)
   - Kasnije dodaj i frontend URL sa Static Web App
3. Klikni **"Save"**

### 4.5 Deploy Backend-a

**Opcija A: Visual Studio**
1. Otvori `backend/ProjectOrganizer.Api` u Visual Studio
2. Right-click na projekat → **"Publish"**
3. Target: **"Azure"** → **"Azure App Service (Linux)"**
4. Izaberi subscription → Izaberi `projectorganizer-api`
5. Klikni **"Finish"** → **"Publish"**

**Opcija B: VS Code**
1. Instaliraj extension: **"Azure App Service"**
2. Right-click na projekat → **"Deploy to Web App"**
3. Izaberi `projectorganizer-api`

**Opcija C: ZIP Deploy (ručno)**
1. Build backend lokalno:
   ```powershell
   cd backend\ProjectOrganizer.Api
   dotnet publish -c Release -o .\publish
   ```
2. Zipuj `publish` folder
3. U Azure Portal → App Service → **"Deployment Center"**
4. Upload ZIP file

✅ Backend URL: `https://projectorganizer-api.azurewebsites.net`

Testiraj: `https://projectorganizer-api.azurewebsites.net/swagger`

---

## 🎨 Korak 5: Frontend - Static Web App

### 5.1 Ažuriraj production config

Izmeni `frontend/src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://projectorganizer-api.azurewebsites.net/api',
  auth0: {
    domain: '[TVOJ_AUTH0_DOMAIN]',
    clientId: '[TVOJ_AUTH0_CLIENT_ID]',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: '[TVOJ_AUTH0_AUDIENCE]'
    }
  }
};
```

### 5.2 Build frontend

```powershell
cd frontend
npm run build -- --configuration production
```

Build će biti u `dist/frontend/browser/` folderu.

### 5.3 Kreiraj Static Web App

1. Klikni **"Create a resource"**
2. Pretraži: `Static Web App`
3. Klikni **"Create"**
4. Popuni:

   **Basics tab:**
   - **Resource Group:** `ProjectOrganizerRG`
   - **Name:** `projectorganizer-frontend`
   - **Plan type:** Free
   - **Region:** West Europe
   - **Deployment details:**
     - **Source:** Other (za ručni upload)

5. Klikni **"Review + Create"** → **"Create"**

### 5.4 Deploy Frontend-a

**Opcija A: Azure CLI (nakon restarta terminala)**
```powershell
az staticwebapp deploy `
  --name projectorganizer-frontend `
  --resource-group ProjectOrganizerRG `
  --app-location "frontend/dist/frontend/browser"
```

**Opcija B: VS Code Extension**
1. Instaliraj: **"Azure Static Web Apps"** extension
2. Right-click na `dist/frontend/browser` → **"Deploy to Static Web App"**

**Opcija C: GitHub Actions (najbolje za produkciju)**
1. U Static Web App → **"Deployment"** → Klikni na **"GitHub"**
2. Autorizuj GitHub
3. Izaberi repo i branch
4. Build Details:
   - **App location:** `/frontend`
   - **Output location:** `dist/frontend/browser`

✅ Frontend URL: `https://[RANDOM-NAME].azurestaticapps.net`

Naći ćeš tačan URL u Static Web App → **"Overview"** → **"URL"**

---

## 🔐 Korak 6: Ažuriraj Auth0

1. Idi na Auth0 Dashboard → **Applications** → Tvoja aplikacija
2. U **Settings**:
   - **Allowed Callback URLs:** Dodaj:
     ```
     https://[STATIC_WEB_APP_URL]
     ```
   - **Allowed Logout URLs:** Dodaj:
     ```
     https://[STATIC_WEB_APP_URL]
     ```
   - **Allowed Web Origins:** Dodaj:
     ```
     https://[STATIC_WEB_APP_URL]
     ```
3. **Save Changes**

### Ažuriraj CORS na Backend-u

1. Vrati se u App Service → **"CORS"**
2. Dodaj Static Web App URL:
   ```
   https://[STATIC_WEB_APP_URL]
   ```
3. **Save**

---

## ✅ Testiranje

1. Otvori: `https://[STATIC_WEB_APP_URL]`
2. Klikni **"Prijavi se"**
3. Loguj se sa Auth0
4. Testiraj kreiranje projekta i klijenta

---

## 💰 Trošak (mesečno)

| Resurs | Plan | Cena |
|--------|------|------|
| SQL Database | Basic | ~$5 |
| App Service | B1 | ~$13 |
| Static Web App | Free | $0 |
| **Ukupno** | | **~$18/mesec** |

💡 **Za Free tier:**
- SQL Database: Idi u Basic (najjeftinije)
- App Service: F1 (free, ali ima limitacije)

---

## 🎯 Quick Links

Nakon kreiranja, sačuvaj ove URL-ove:

- **Frontend:** `https://[STATIC_WEB_APP].azurestaticapps.net`
- **Backend:** `https://projectorganizer-api.azurewebsites.net`
- **Swagger:** `https://projectorganizer-api.azurewebsites.net/swagger`
- **Azure Portal:** https://portal.azure.com → Resource Group: `ProjectOrganizerRG`

---

## 🐛 Troubleshooting

### Problem: Backend vraća 500 error
- Proveri Connection String u Configuration
- Proveri Auth0 settings
- Pogledaj logove: App Service → **"Log stream"**

### Problem: Frontend ne može da se poveže na backend
- Proveri CORS u App Service
- Proveri da li je backend URL tačan u `environment.prod.ts`

### Problem: Auth0 redirect ne radi
- Proveri Allowed URLs u Auth0 Dashboard
- Proveri da li su tačni domain i clientId u frontendu

---

**Gotovo! Aplikacija je live na Azure! 🚀**

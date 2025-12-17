# Azure Deployment Guide - Project Organizer

Kompletan vodič za deployment na Azure cloud.

## 📋 Šta ćemo kreirati na Azure:

1. **Azure SQL Database** - Baza podataka
2. **Azure App Service** - Backend API (.NET)
3. **Azure Static Web App** - Frontend (Angular)

---

## 🚀 Korak 1: Priprema

### Instaliraj Azure CLI

```bash
# Windows (PowerShell)
winget install -e --id Microsoft.AzureCLI

# Ili preuzmi sa: https://aka.ms/installazurecliwindows
```

### Loguj se na Azure

```bash
az login
```

### Kreiraj Resource Group

```bash
az group create --name ProjectOrganizerRG --location westeurope
```

---

## 🗄️ Korak 2: Azure SQL Database

### 2.1 Kreiraj SQL Server

```bash
az sql server create \
  --name projectorganizer-sql-server \
  --resource-group ProjectOrganizerRG \
  --location westeurope \
  --admin-user sqladmin \
  --admin-password "PromeniOvuLozinku123!"
```

**⚠️ VAŽNO:** Promeni `admin-password` u nešto sigurnije!

### 2.2 Konfiguriši Firewall (dozvoli Azure servise)

```bash
az sql server firewall-rule create \
  --resource-group ProjectOrganizerRG \
  --server projectorganizer-sql-server \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 2.3 Dozvoli tvoj IP (za lokalni pristup)

```bash
az sql server firewall-rule create \
  --resource-group ProjectOrganizerRG \
  --server projectorganizer-sql-server \
  --name AllowMyIP \
  --start-ip-address [TVOJ_IP] \
  --end-ip-address [TVOJ_IP]
```

### 2.4 Kreiraj bazu

```bash
az sql db create \
  --resource-group ProjectOrganizerRG \
  --server projectorganizer-sql-server \
  --name ProjectOrganizer \
  --service-objective Basic \
  --backup-storage-redundancy Local
```

### 2.5 Pokreni SQL skripte na Azure SQL

**Connection String za SSMS:**
```
Server=projectorganizer-sql-server.database.windows.net,1433
Database=ProjectOrganizer
User ID=sqladmin
Password=[TVOJA_LOZINKA]
```

Pokreni SQL skripte iz `database/` foldera redom:
1. `02_CreateTables.sql` (bez 01_CreateDatabase.sql jer je baza već kreirana)
2. `03_SeedData.sql`

---

## ⚙️ Korak 3: Backend - Azure App Service

### 3.1 Kreiraj App Service Plan

```bash
az appservice plan create \
  --name ProjectOrganizerPlan \
  --resource-group ProjectOrganizerRG \
  --sku B1 \
  --is-linux
```

### 3.2 Kreiraj Web App

```bash
az webapp create \
  --name projectorganizer-api \
  --resource-group ProjectOrganizerRG \
  --plan ProjectOrganizerPlan \
  --runtime "DOTNET|8.0"
```

### 3.3 Konfiguriši Connection String

```bash
az webapp config connection-string set \
  --name projectorganizer-api \
  --resource-group ProjectOrganizerRG \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:projectorganizer-sql-server.database.windows.net,1433;Initial Catalog=ProjectOrganizer;Persist Security Info=False;User ID=sqladmin;Password=[TVOJA_LOZINKA];MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### 3.4 Konfiguriši Auth0 settings

```bash
az webapp config appsettings set \
  --name projectorganizer-api \
  --resource-group ProjectOrganizerRG \
  --settings \
    Auth0__Domain="[TVOJ_AUTH0_DOMAIN]" \
    Auth0__Audience="[TVOJ_AUTH0_AUDIENCE]"
```

### 3.5 Omogući CORS

```bash
az webapp cors add \
  --name projectorganizer-api \
  --resource-group ProjectOrganizerRG \
  --allowed-origins "https://[TVOJ_STATIC_WEB_APP].azurestaticapps.net"
```

### 3.6 Deploy Backend-a

**Opcija 1: Publish iz Visual Studio**
1. Right-click na projekat → Publish
2. Izaberi Azure → Azure App Service (Linux)
3. Izaberi `projectorganizer-api`
4. Publish

**Opcija 2: CLI Deployment**

```bash
cd backend/ProjectOrganizer.Api

# Build
dotnet publish -c Release -o ./publish

# Deploy (potreban zip)
cd publish
powershell Compress-Archive -Path * -DestinationPath ../app.zip -Force
cd ..

az webapp deployment source config-zip \
  --resource-group ProjectOrganizerRG \
  --name projectorganizer-api \
  --src app.zip
```

**Backend URL:** `https://projectorganizer-api.azurewebsites.net`

---

## 🎨 Korak 4: Frontend - Azure Static Web App

### 4.1 Build Frontend-a

```bash
cd frontend
npm run build
```

### 4.2 Kreiraj Static Web App

**Opcija 1: Preko Azure Portal**
1. Idi na Azure Portal → Create a resource → Static Web App
2. Resource Group: `ProjectOrganizerRG`
3. Name: `projectorganizer-frontend`
4. Region: `West Europe`
5. Deployment: Other (manual)
6. Create

**Opcija 2: CLI**

```bash
az staticwebapp create \
  --name projectorganizer-frontend \
  --resource-group ProjectOrganizerRG \
  --location westeurope \
  --source .
```

### 4.3 Ažuriraj environment.prod.ts

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

### 4.4 Rebuild i Deploy

```bash
# Build sa production config
npm run build -- --configuration production

# Deploy
az staticwebapp deploy \
  --name projectorganizer-frontend \
  --resource-group ProjectOrganizerRG \
  --app-location "dist/frontend/browser"
```

**Frontend URL:** `https://[GENERATED_NAME].azurestaticapps.net`

---

## 🔐 Korak 5: Ažuriraj Auth0

### Update Allowed URLs u Auth0 Dashboard:

**Application Settings:**
- Allowed Callback URLs: `https://[STATIC_WEB_APP_URL], http://localhost:4200`
- Allowed Logout URLs: `https://[STATIC_WEB_APP_URL], http://localhost:4200`
- Allowed Web Origins: `https://[STATIC_WEB_APP_URL], http://localhost:4200`

---

## ✅ Verifikacija

### Backend provera:
```bash
curl https://projectorganizer-api.azurewebsites.net/api/projekti
```

Ili otvori Swagger: `https://projectorganizer-api.azurewebsites.net/swagger`

### Frontend provera:
Otvori u browseru: `https://[TVOJ_STATIC_WEB_APP].azurestaticapps.net`

---

## 💰 Cene (mesečno)

| Resurs | SKU | Cena |
|--------|-----|------|
| SQL Database | Basic | ~$5 |
| App Service | B1 | ~$13 |
| Static Web App | Free | $0 |
| **Ukupno** | | **~$18/mesec** |

---

## 📊 Monitoring

### Omogući Application Insights

```bash
az monitor app-insights component create \
  --app projectorganizer-insights \
  --location westeurope \
  --resource-group ProjectOrganizerRG \
  --application-type web

# Poveži sa App Service
az webapp config appsettings set \
  --name projectorganizer-api \
  --resource-group ProjectOrganizerRG \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="[CONNECTION_STRING]"
```

---

## 🔄 CI/CD - GitHub Actions (Opciono)

### Backend GitHub Actions workflow:

`.github/workflows/backend-deploy.yml`:

```yaml
name: Deploy Backend to Azure

on:
  push:
    branches: [ main ]
    paths:
      - 'backend/**'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: |
        cd backend/ProjectOrganizer.Api
        dotnet publish -c Release -o publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'projectorganizer-api'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: backend/ProjectOrganizer.Api/publish
```

### Frontend GitHub Actions workflow:

`.github/workflows/frontend-deploy.yml`:

```yaml
name: Deploy Frontend to Azure Static Web Apps

on:
  push:
    branches: [ main ]
    paths:
      - 'frontend/**'

jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup Node.js
      uses: actions/setup-node@v2
      with:
        node-version: '20'
    
    - name: Install and Build
      run: |
        cd frontend
        npm install
        npm run build -- --configuration production
    
    - name: Deploy to Azure Static Web Apps
      uses: Azure/static-web-apps-deploy@v1
      with:
        azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        action: "upload"
        app_location: "frontend"
        output_location: "dist/frontend/browser"
```

---

## 🛠️ Troubleshooting

### Problem: Backend ne može da se poveže na bazu

**Rešenje:** Proveri firewall rules na SQL Server-u

```bash
az sql server firewall-rule list \
  --resource-group ProjectOrganizerRG \
  --server projectorganizer-sql-server
```

### Problem: CORS greška

**Rešenje:** Ažuriraj CORS settings na App Service

### Problem: Auth0 login redirect ne radi

**Rešenje:** Proveri Allowed URLs u Auth0 Dashboard

---

## 📝 Korisni linkovi

- Azure Portal: https://portal.azure.com
- Azure SQL: `https://portal.azure.com/#@/resource/subscriptions/[SUB_ID]/resourceGroups/ProjectOrganizerRG/providers/Microsoft.Sql/servers/projectorganizer-sql-server`
- App Service: `https://portal.azure.com/#@/resource/subscriptions/[SUB_ID]/resourceGroups/ProjectOrganizerRG/providers/Microsoft.Web/sites/projectorganizer-api`
- Static Web App: `https://portal.azure.com/#@/resource/subscriptions/[SUB_ID]/resourceGroups/ProjectOrganizerRG/providers/Microsoft.Web/staticSites/projectorganizer-frontend`

---

**Sve je spremno! 🚀**

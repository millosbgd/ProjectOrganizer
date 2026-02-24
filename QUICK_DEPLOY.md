# 🚀 Quick Deploy Cheat Sheet

Najčešće korišćene komande za deployment.

---

## Frontend Deploy (Automatic via GitHub)

```powershell
git add .
git commit -m "Your message"
git push origin deploy
# Wait for GitHub Actions to complete
# URL: https://delightful-pebble-0fc0d9403.4.azurestaticapps.net
```

---

## Backend Deploy (Manual)

```powershell
# Automatski (preporučeno):
.\deploy-backend.ps1

# Manualno:
cd backend/ProjectOrganizer.Api
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath ../app.zip -Force
cd ..
az webapp deployment source config-zip --resource-group rg-projectorganizer-dev --name ProjectOrganizer --src app.zip
```

---

## Database Migrations (Azure SQL)

**SSMS Connection:**
- Server: `projectorganizer-sql.database.windows.net`
- Database: `ProjectOrganizer`
- Auth: SQL Server Authentication

**Run SQL scripts in order from `database/` folder**

---

## Complete Deployment (All Changes)

```powershell
# 1. Commit
git add .
git commit -m "Description"
git push origin deploy

# 2. SQL Migration (if needed)
# Run SQL scripts in SSMS on Azure SQL

# 3. Backend Deploy
.\deploy-backend.ps1

# 4. Frontend deploys automatically via GitHub Actions

# 5. Test
# https://delightful-pebble-0fc0d9403.4.azurestaticapps.net
# https://projectorganizer.azurewebsites.net/api
```

---

## Deployment Order (CRITICAL!)

Always follow this order:
1. **Database** (SQL migrations first)
2. **Backend** (API second)
3. **Frontend** (UI last - automatic)

**Why?** Backend needs DB schema, Frontend needs API endpoints.

---

## Useful Commands

```powershell
# Check Azure login
az account show

# Login to Azure
az login

# Check App Service logs
az webapp log tail --resource-group rg-projectorganizer-dev --name ProjectOrganizer

# Restart backend
az webapp restart --resource-group rg-projectorganizer-dev --name ProjectOrganizer

# List all resources
az resource list --resource-group rg-projectorganizer-dev --output table

# Build backend locally
cd backend/ProjectOrganizer.Api
dotnet build
dotnet run

# Build frontend locally
cd frontend
npm install
ng serve
```

---

## Production URLs

- **Frontend:** https://delightful-pebble-0fc0d9403.4.azurestaticapps.net
- **Backend:** https://projectorganizer.azurewebsites.net
- **API:** https://projectorganizer.azurewebsites.net/api
- **Database:** projectorganizer-sql.database.windows.net

---

## Troubleshooting

**Frontend not updating?**
```
Ctrl + Shift + R (hard refresh)
Check GitHub Actions status
```

**Backend errors?**
```powershell
az webapp log tail --resource-group rg-projectorganizer-dev --name ProjectOrganizer
```

**Database connection issues?**
```
Check firewall rules in Azure Portal
Verify connection string in App Service settings
```

---

**💡 Tip:** Bookmark this file for quick reference!

# Project Organizer API

ASP.NET Core Web API za upravljanje projektima, klijentima i aktivnostima.

## Tehnologije

- .NET 8
- Entity Framework Core
- SQL Server
- Auth0 Authentication
- Swagger/OpenAPI

## Setup

### Preduslovi

- .NET 8 SDK
- SQL Server (lokal ili Azure)
- Visual Studio 2022 ili VS Code

### Instalacija

1. Kreiraj bazu podataka izvršavanjem SQL skripti iz `database` foldera:
   ```powershell
   sqlcmd -S MILOS-LAPTOP -U sa -P sql -i ../database/01_CreateDatabase.sql
   sqlcmd -S MILOS-LAPTOP -U sa -P sql -i ../database/02_CreateTables.sql
   sqlcmd -S MILOS-LAPTOP -U sa -P sql -i ../database/03_SeedData.sql
   ```

2. Konfiguriši `appsettings.json`:
   - Postavi Auth0 kredencijale (Domain i Audience)
   - Proveri connection string za lokalno okruženje
   - Za produkciju, postavi `Environment` na "Production" i ažuriraj production connection string

3. Instaliraj dependencies:
   ```powershell
   cd backend/ProjectOrganizer.Api
   dotnet restore
   ```

4. Pokreni aplikaciju:
   ```powershell
   dotnet run
   ```

API će biti dostupan na `https://localhost:7000` (ili port koji .NET dodeli).

## Auth0 Setup

1. Kreiraj Auth0 nalog na [auth0.com](https://auth0.com)
2. Kreiraj novi API u Auth0 dashboard-u
3. Kopiraj Domain i API Identifier u `appsettings.json`
4. Konfiguriši Allowed Callback URLs za Angular app

## Swagger

Swagger UI je dostupan na `/swagger` endpoint kada aplikacija radi u Development mode.

## Endpoints

### Projekti
- `GET /api/Projekti` - Lista svih projekata
- `GET /api/Projekti/{id}` - Detalji projekta
- `POST /api/Projekti` - Kreiraj novi projekat
- `PUT /api/Projekti/{id}` - Ažuriraj projekat
- `DELETE /api/Projekti/{id}` - Obriši projekat

### Klijenti
- `GET /api/Klijenti` - Lista svih klijenata
- `GET /api/Klijenti/{id}` - Detalji klijenta
- `POST /api/Klijenti` - Kreiraj novog klijenta
- `PUT /api/Klijenti/{id}` - Ažuriraj klijenta
- `DELETE /api/Klijenti/{id}` - Obriši klijenta

### Aktivnosti
- `GET /api/Aktivnosti` - Lista svih aktivnosti
- `GET /api/Aktivnosti/{id}` - Detalji aktivnosti
- `POST /api/Aktivnosti` - Kreiraj novu aktivnost
- `PUT /api/Aktivnosti/{id}` - Ažuriraj aktivnost
- `DELETE /api/Aktivnosti/{id}` - Obriši aktivnost

## Deployment na Azure

### Via GitHub Actions

1. Kreiraj Azure App Service
2. Konfiguriši GitHub Secrets:
   - `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Connection string za Azure SQL Database
3. Push na `main` branch će automatski deployovati aplikaciju

### Manual Deployment

```powershell
# Publish aplikacije
dotnet publish -c Release -o ./publish

# Deploy na Azure (ili kopiraj fajlove na server)
```

## Environment Configuration

Promeni `Environment` u `appsettings.json` za switch između Local i Production:

```json
{
  "Environment": "Local"  // ili "Production"
}
```

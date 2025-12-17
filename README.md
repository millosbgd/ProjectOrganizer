# Project Organizer

Sistem za upravljanje projektima, klijentima i aktivnostima sa odvojenim frontendom (Angular) i backendom (C# .NET).

## 📋 Opis projekta

Aplikacija omogućava:
- Upravljanje projektima sa detaljnim informacijama
- Dodavanje aktivnosti na projekte sa različitim statusima i vrstama
- Vođenje evidencije klijenata
- Autentifikaciju korisnika preko Auth0
- Deployment na Azure cloud

## 🏗️ Tehnološki Stack

### Frontend
- **Framework:** Angular 18
- **Autentifikacija:** Auth0
- **Jezik:** TypeScript
- **Stilizovanje:** CSS

### Backend
- **Framework:** .NET 8 Web API
- **ORM:** Entity Framework Core
- **Autentifikacija:** JWT Bearer (Auth0)
- **API dokumentacija:** Swagger/OpenAPI

### Baza podataka
- **RDBMS:** Microsoft SQL Server
- **Server (Local):** MILOS-LAPTOP
- **Baza:** ProjectOrganizer
- **User:** sa
- **Pass:** sql

## 📁 Struktura projekta

```
ProjectOrganizer/
├── frontend/                 # Angular aplikacija
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/   # UI komponente
│   │   │   ├── models/       # TypeScript modeli
│   │   │   ├── services/     # API servisi
│   │   │   └── ...
│   │   └── environments/     # Konfiguracija okruženja
│   └── README.md
│
├── backend/                  # .NET Web API
│   └── ProjectOrganizer.Api/
│       ├── Controllers/      # API kontroleri
│       ├── Data/            # DbContext i migracije
│       ├── Models/          # C# modeli
│       └── README.md
│
└── database/                 # SQL skripta
    ├── 01_CreateDatabase.sql
    ├── 02_CreateTables.sql
    ├── 03_SeedData.sql
    └── README.md
```

## 🗄️ Model baze podataka

### Tabele

#### Projekti
- `Id` (int, PK)
- `BrojProjekta` (string)
- `Datum` (datetime)
- `Naziv` (string)
- `Aktivan` (boolean)
- `Status` (string)
- `KlijentId` (int, FK)

#### Klijenti
- `Id` (int, PK)
- `Naziv` (string)
- `Adresa` (string)
- `Grad` (string)
- `Zemlja` (string)

#### Aktivnosti
- `Id` (int, PK)
- `Opis` (string)
- `Datum` (datetime)
- `Status` (string)
- `Vrsta` (string)
- `ProjekatId` (int, FK)

## 🚀 Brzo pokretanje

### 1. Baza podataka

```bash
# Pokreni SQL skripte iz database/ foldera
```

1. `01_CreateDatabase.sql` - Kreira bazu
2. `02_CreateTables.sql` - Kreira tabele
3. `03_SeedData.sql` - Ubacuje test podatke

### 2. Backend

```bash
cd backend/ProjectOrganizer.Api
dotnet restore
dotnet run
```

Backend će biti dostupan na: `https://localhost:7256`

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

Frontend će biti dostupan na: `http://localhost:4200`

## ⚙️ Konfiguracija

### Auth0 Setup

1. Kreiraj besplatan nalog na [Auth0](https://auth0.com/)
2. Kreiraj novu **Single Page Application** za frontend
3. Kreiraj **API** za backend
4. Podesi konfiguracione fajlove:

#### Frontend (`frontend/src/environments/environment.ts`):
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7256/api',
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN',
    clientId: 'YOUR_CLIENT_ID',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'YOUR_API_IDENTIFIER'
    }
  }
};
```

#### Backend (`backend/ProjectOrganizer.Api/appsettings.json`):
```json
{
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "Audience": "YOUR_API_IDENTIFIER"
  }
}
```

### Okruženja

Projekat podržava dva okruženja:

1. **Development (Lokalno)**
   - Backend: `https://localhost:7256`
   - Frontend: `http://localhost:4200`
   - Baza: SQL Server na `MILOS-LAPTOP`

2. **Production (Azure)**
   - Backend: Azure App Service
   - Frontend: Azure Static Web App
   - Baza: Azure SQL Database

## 🌐 Deployment na Azure

### Backend (App Service)

1. Kreiraj App Service na Azure portalu
2. Konfiguriši Connection String preko Azure portal
3. Deploy preko GitHub Actions ili Visual Studio

### Frontend (Static Web App)

1. Build aplikacije: `npm run build`
2. Deploy preko Azure Static Web Apps
3. Konfiguriši Custom Domain (opciono)

### GitHub Actions

Projekat se može automatski deployovati preko GitHub Actions:
- Push na `main` branch pokreće deployment

## 🎨 Dizajn

- **Boja:** Svetlo plava (`#5dade2`, `#3498db`)
- **Stil:** Čist, minimalistički
- **Layout:** Responsive grid sistem
- **Header:** Tanak, sa gradijentom

## 📝 API Endpointi

### Projekti
- `GET /api/projekti` - Lista projekata
- `GET /api/projekti/{id}` - Detalji projekta
- `POST /api/projekti` - Kreiranje
- `PUT /api/projekti/{id}` - Ažuriranje
- `DELETE /api/projekti/{id}` - Brisanje

### Klijenti
- `GET /api/klijenti` - Lista klijenata
- `POST /api/klijenti` - Kreiranje
- itd...

### Aktivnosti
- `GET /api/aktivnosti/projekat/{projekatId}` - Aktivnosti po projektu
- `POST /api/aktivnosti` - Kreiranje
- itd...

## 📚 Dodatne informacije

- **Frontend README:** [frontend/README.md](frontend/README.md)
- **Backend README:** [backend/README.md](backend/README.md)
- **Database README:** [database/README.md](database/README.md)

## 🔐 Sigurnost

- Sve API rute su zaštićene JWT Bearer tokenima (Auth0)
- Frontend sadrži Auth Guard za zaštitu ruta
- SQL injekcije su sprečene korišćenjem Entity Framework parametrizovanih upita

## 📄 Licenca

Privatni projekat - Sva prava zadržana

## 👤 Autor

Milos Novakovic

---

**Napomena:** Pre prvog pokretanja, obavezno konfiguriši Auth0 i connection stringove!

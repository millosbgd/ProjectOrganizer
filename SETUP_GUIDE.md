# Project Organizer - Setup Guide

## 🎯 Kompletna instalacija projekta

### Faza 1: Baza podataka ✅

Baza je već kreirana i test podaci su uneti.

**Status:** ✅ Gotovo
- Server: MILOS-LAPTOP
- Baza: ProjectOrganizer
- Tabele: Projekti, Klijenti, Aktivnosti

### Faza 2: Backend (.NET API) ✅

Backend je konfigurisan i spreman za rad.

**Status:** ✅ Radi na `http://localhost:5000`

### Faza 3: Frontend (Angular) ✅

Frontend struktura je kreirana sa svim komponentama.

**Status:** ✅ Struktura kompletna

---

## 🔐 Sledeći koraci - Auth0 Konfiguracija

### Korak 1: Kreiranje Auth0 naloga

1. Idi na https://auth0.com/
2. Klikni "Sign up" i kreiraj besplatan nalog
3. Verifikuj email adresu

### Korak 2: Kreiranje Auth0 API-ja (za Backend)

1. U Auth0 Dashboard-u, idi na **Applications → APIs**
2. Klikni **"Create API"**
3. Popuni podatke:
   - **Name:** ProjectOrganizer API
   - **Identifier:** `https://projectorganizer.api` (ovo će biti tvoj `audience`)
   - **Signing Algorithm:** RS256
4. Klikni **"Create"**
5. **Sačuvaj `Identifier`** - trebaće ti za konfiguraciju

### Korak 3: Kreiranje Auth0 Application (za Frontend)

1. U Auth0 Dashboard-u, idi na **Applications → Applications**
2. Klikni **"Create Application"**
3. Popuni podatke:
   - **Name:** ProjectOrganizer Frontend
   - **Application Type:** Single Page Application
4. Klikni **"Create"**
5. U **Settings** tabu, popuni:
   - **Allowed Callback URLs:** `http://localhost:4200, http://localhost:4200/callback`
   - **Allowed Logout URLs:** `http://localhost:4200`
   - **Allowed Web Origins:** `http://localhost:4200`
6. Klikni **"Save Changes"**
7. **Sačuvaj sledeće vrednosti:**
   - Domain (npr. `dev-xxxxxxx.us.auth0.com`)
   - Client ID (npr. `AbCdEfGhIjKlMnOpQrStUvWxYz`)

### Korak 4: Konfiguracija Backend-a

Izmeni fajl: `backend/ProjectOrganizer.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=MILOS-LAPTOP;Database=ProjectOrganizer;User Id=sa;Password=sql;TrustServerCertificate=True;"
  },
  "Auth0": {
    "Domain": "dev-xxxxxxx.us.auth0.com",  // Tvoj Auth0 Domain
    "Audience": "https://projectorganizer.api"  // API Identifier iz Koraka 2
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Korak 5: Konfiguracija Frontend-a

Izmeni fajl: `frontend/src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  auth0: {
    domain: 'dev-xxxxxxx.us.auth0.com',  // Tvoj Auth0 Domain
    clientId: 'AbCdEfGhIjKlMnOpQrStUvWxYz',  // Client ID iz Koraka 3
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://projectorganizer.api'  // API Identifier iz Koraka 2
    }
  }
};
```

---

## 🚀 Pokretanje kompletnog sistema

### 1. Pokreni Backend

```bash
cd backend/ProjectOrganizer.Api
dotnet run
```

✅ Backend je dostupan na: **http://localhost:5000**  
📄 Swagger UI: **http://localhost:5000/swagger**

### 2. Pokreni Frontend

```bash
cd frontend
npm start
```

✅ Frontend je dostupan na: **http://localhost:4200**

### 3. Testiraj aplikaciju

1. Otvori **http://localhost:4200** u browseru
2. Klikni **"Prijavi se"** (koristi Auth0 login)
3. Kreiraj test nalog ili koristi social login (Google, GitHub, itd.)
4. Nakon logovanja, trebalo bi da vidiš listu projekata

---

## 📋 Checklist

- [ ] Kreiran Auth0 nalog
- [ ] Kreiran Auth0 API (Backend)
- [ ] Kreirana Auth0 Single Page Application (Frontend)
- [ ] Konfigurisan `appsettings.json` sa Auth0 podacima
- [ ] Konfigurisan `environment.ts` sa Auth0 podacima
- [ ] Backend pokrenut na `http://localhost:5000`
- [ ] Frontend pokrenut na `http://localhost:4200`
- [ ] Testiran login
- [ ] Testiran CRUD za projekte

---

## 🐛 Troubleshooting

### Problem: CORS greška u browseru

**Rešenje:** Backend već ima CORS konfigurisano za `http://localhost:4200`

### Problem: 401 Unauthorized pri API pozivima

**Rešenje:** 
- Proveri da li si se uspešno ulogovao
- Proveri da li je `audience` isti u frontend i backend konfiguraciji
- Proveri da li je Auth0 API pravilno konfigurisan

### Problem: "Invalid Domain" pri loginu

**Rešenje:** Proveri da li si dobro uneo `domain` u `environment.ts` (bez `https://`)

### Problem: Backend ne može da se poveže na bazu

**Rešenje:** Proveri da li SQL Server radi i da li su kredencijali tačni

---

## 🌐 Production Deployment (Azure)

Kada aplikacija radi lokalno, sledeći korak je deployment na Azure:

### Backend → Azure App Service
### Frontend → Azure Static Web App  
### Baza → Azure SQL Database

Detaljne instrukcije za deployment su u glavnom `README.md`.

---

## 📞 Potrebna pomoć?

Pogledaj:
- [Frontend README](frontend/README.md)
- [Backend README](backend/README.md)
- [Database README](database/README.md)
- [Auth0 Documentation](https://auth0.com/docs)

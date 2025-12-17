# Quick Start - Project Organizer

## ⚡ Brzo pokretanje sistema (već konfigurisano)

### Ako si već podesio Auth0:

#### Terminal 1 - Backend
```bash
cd backend/ProjectOrganizer.Api
dotnet run
```

#### Terminal 2 - Frontend
```bash
cd frontend
npm start
```

### Ako NISI podesio Auth0:

Pročitaj **SETUP_GUIDE.md** za detaljne instrukcije o konfiguraciji Auth0.

---

## 🔍 Brza provera

### Backend provera:
```bash
curl http://localhost:5000/api/projekti
```

Ili otvori u browseru: http://localhost:5000/swagger

### Frontend provera:
Otvori: http://localhost:4200

---

## 📊 Status servisa

| Servis | URL | Status |
|--------|-----|--------|
| Backend API | http://localhost:5000 | ✅ |
| Swagger UI | http://localhost:5000/swagger | ✅ |
| Frontend | http://localhost:4200 | ✅ |
| SQL Server | MILOS-LAPTOP | ✅ |

---

## 🎯 Test korisnici

Nakon što podesiš Auth0, možeš koristiti:
- Google login
- GitHub login
- Email/password (kreiraj nalog u Auth0)

---

## 🛑 Zaustavljanje servisa

U svakom terminalu pritisni `Ctrl + C`

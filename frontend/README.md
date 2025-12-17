# Project Organizer - Frontend

Angular aplikacija za upravljanje projektima, klijentima i aktivnostima.

## Tehnologije

- Angular 18
- Auth0 (autentifikacija)
- TypeScript
- CSS

## Instalacija i pokretanje

### Preduslovi
- Node.js 20.17 ili noviji
- npm

### Instalacija paketa

```bash
cd frontend
npm install
```

### Konfiguracija

Izmeni fajlove za konfiguraciju okruženja:

#### Development (lokalno okruženje)
Fajl: `src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7256/api',  // Lokalni backend
  auth0: {
    domain: 'TVOJ_AUTH0_DOMAIN.auth0.com',
    clientId: 'TVOJ_AUTH0_CLIENT_ID',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'TVOJ_AUTH0_API_IDENTIFIER'
    }
  }
};
```

#### Production (Azure okruženje)
Fajl: `src/environments/environment.prod.ts`

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://TVOJA_AZURE_APP.azurewebsites.net/api',
  auth0: {
    domain: 'TVOJ_AUTH0_DOMAIN.auth0.com',
    clientId: 'TVOJ_AUTH0_CLIENT_ID',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'TVOJ_AUTH0_API_IDENTIFIER'
    }
  }
};
```

### Auth0 Konfiguracija

1. Napravi nalog na [Auth0](https://auth0.com/)
2. Kreiraj novu aplikaciju (Single Page Application)
3. Podesi Allowed Callback URLs: `http://localhost:4200, https://TVOJ_AZURE_URL`
4. Podesi Allowed Logout URLs: `http://localhost:4200, https://TVOJ_AZURE_URL`
5. Podesi Allowed Web Origins: `http://localhost:4200, https://TVOJ_AZURE_URL`
6. Kreiraj API u Auth0 Dashboard i iskoristi Identifier kao `audience`

### Pokretanje aplikacije

Development mode:
```bash
npm start
```

Aplikacija će biti dostupna na `http://localhost:4200`

### Build za produkciju

```bash
npm run build
```

Build fajlovi će biti u `dist/` folderu.

## Struktura projekta

```
src/
├── app/
│   ├── components/         # Angular komponente
│   │   ├── header/
│   │   ├── projekti-list/
│   │   └── projekat-detail/
│   ├── models/            # TypeScript interfejsi
│   │   ├── projekat.model.ts
│   │   ├── klijent.model.ts
│   │   └── aktivnost.model.ts
│   ├── services/          # API servisi
│   │   ├── projekat.service.ts
│   │   ├── klijent.service.ts
│   │   └── aktivnost.service.ts
│   ├── app.component.ts
│   ├── app.config.ts
│   └── app.routes.ts
├── environments/          # Konfiguracija okruženja
│   ├── environment.ts
│   └── environment.prod.ts
└── styles.css            # Globalni stilovi
```

## Funkcionalnosti

- ✅ Autentifikacija preko Auth0
- ✅ Pregled projekata (lista i grid)
- ✅ Kreiranje/izmena/brisanje projekata
- ✅ Pregled detalja projekta
- ✅ Dodavanje aktivnosti na projekte
- ✅ Povezivanje projekata sa klijentima
- ✅ Responsive dizajn
- ✅ Svetlo plavi header sa laganjem izgledom

## Deployment na Azure

### Static Web App (preporučeno za Angular)

1. Instaliraj Azure Static Web Apps CLI:
```bash
npm install -g @azure/static-web-apps-cli
```

2. Build aplikacije:
```bash
npm run build
```

3. Deploy preko Azure portal ili GitHub Actions

## Pomoć

Za pomoć oko backenда pogledaj `../backend/README.md`

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

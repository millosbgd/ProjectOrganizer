# Plan za Redesign Navigacije

## 📋 Cilj
Promena navigacije aplikacije sa horizontalnog top bar-a na **collapsable sidebar sa leve strane**, uz zadržavanje funkcionalnosti i dodavanje novog modernog izgleda.

---

## 🎨 Dizajn Specifikacije

### Primarna Boja
- **Plava**: `#00a1ff` (svetlo plava)
- **Bela**: `#ffffff`
- **Hover pozadinа**: `rgba(0, 161, 255, 0.05)` ili svetlo siva
- **Border boja**: `#00a1ff`
- **Active/Hover tamnija**: `#0080cc`

### Fontovi
- **Google Fonts**: Roboto ili Inter (za moderan izgled)
- **Ikone**: Material Icons (Google Material Design Icons) - SVG format

---

## 🏗️ Struktura Komponenti

### 1. **Nova Komponenta: `sidebar-nav`**
   - **Lokacija**: `frontend/src/app/components/sidebar-nav/`
   - **Fajlovi**:
     - `sidebar-nav.component.ts`
     - `sidebar-nav.component.html`
     - `sidebar-nav.component.css`

### 2. **Modifikovana Komponenta: `header`**
   - **Transformacija**: Od punog navigacionog bara → u tanku belu traku
   - **Sadržaj**:
     - Logo ili naziv aplikacije (opciono, levo)
     - Ime korisnika (desno)
     - Logout ikonica (desno)
   - **Border**: Donji border u boji `#00a1ff` (3px)

### 3. **Modifikacija: `app.component`**
   - **Layout**: Treba prilagoditi da uključi sidebar pored main content-a
   - **Grid ili Flexbox**: Za pozicioniranje header + sidebar + main content

---

## 📐 Layout Struktura

```
┌─────────────────────────────────────────────────────────┐
│  HEADER (bela traka, donji border #00a1ff)             │
│  [Logo?]                    [Ime korisnika] [Logout 🚪] │
└─────────────────────────────────────────────────────────┘
┌─────────────────────┬───────────────────────────────────┐
│  SIDEBAR (levo)     │                                   │
│  ┌────────────────┐ │                                   │
│  │ [☰] Toggle btn │ │   MAIN CONTENT                    │
│  │                │ │   (router-outlet)                 │
│  │ 🏠 Projekti    │ │                                   │
│  │ 👥 Klijenti    │ │                                   │
│  │ 📅 Kalendar    │ │                                   │
│  │ 🛠️ Modeli Impl.│ │                                   │
│  │ 👤 Korisnici   │ │                                   │
│  │ ⚙️ Podešavanja │ │                                   │
│  │                │ │                                   │
│  └────────────────┘ │                                   │
└─────────────────────┴───────────────────────────────────┘
```

**Collapsable verzija:**
- Kada je collapsed → pokazuje samo ikone (uži sidebar)
- Kada je expanded → pokazuje ikone + tekst (širi sidebar)

---

## 🎯 Funkcionalnost Sidebar-a

### State Management
- **Property**: `isSidebarCollapsed: boolean = false`
- **Toggle metoda**: `toggleSidebar()`
- **Čuvanje stanja**: LocalStorage (opciono) - da zapamti da li je korisnik ostavio collapsed ili expanded

### Navigacioni Linkovi
Isti linkovi kao trenutno:
1. 🏠 **Projekti** → `/projekti`
2. 👥 **Klijenti** → `/klijenti`
3. 📅 **Kalendar** → `/kalendar`
4. 🛠️ **Modeli Implementacije** → `/implementation-models`
5. 👤 **Korisnici** → `/admin/users`
6. ⚙️ **Podešavanja** → `/settings`

### Interakcija
- **Click na link**: Navigacija na odgovarajuću rutu
- **Hover efekat**: Svetlo siva border ili pozadina
- **Active state**: Jači highlight (npr. `background: rgba(255, 255, 255, 0.2)`)
- **Toggle dugme**: Na vrhu sidebar-a za collapse/expand

---

## 🎨 Material Icons (SVG)

### Predložene Ikone:
- **Projekti**: `folder` ili `work`
- **Klijenti**: `people` ili `contacts`
- **Kalendar**: `calendar_today` ili `event`
- **Modeli Implementacije**: `settings` ili `build`
- **Korisnici**: `person` ili `manage_accounts`
- **Podešavanja**: `settings` ili `tune`
- **Logout**: `logout` ili `exit_to_app`

### Način Upotrebe:
- Link: `<link href="https://fonts.googleapis.com/icon?family=Material+Icons" rel="stylesheet">`
- U HTML-u: `<span class="material-icons">folder</span>`
- Ili koristimo SVG direktno (bolji control)

---

## 🛠️ Implementacioni Koraci

### **Faza 1: Priprema**
1. ✅ Dodaj Google Font (Roboto/Inter) u `index.html`
2. ✅ Dodaj Material Icons u `index.html`
3. ✅ Kreiraj sidebar-nav komponentu
4. ✅ Pripremi SVG ikone ili Material Icons

### **Faza 2: Sidebar Komponenta**
1. ✅ Kreiraj strukturu HTML sa svim linkovima i ikonama
2. ✅ Dodaj toggle dugme na vrhu
3. ✅ Implementiraj collapsible logiku
4. ✅ Stilizuj CSS:
   - Pozicioniranje (fixed, levo)
   - Bela pozadina (#ffffff)
   - Plavi tekst i ikone (#00a1ff)
   - Hover efekti
   - Animacije za collapse/expand
   - Width: expanded (npr. 250px), collapsed (npr. 70px)

### **Faza 3: Header Modifikacija**
1. ✅ Ukloni postojeće navigacione linkove iz header-a
2. ✅ Zadrži samo:
   - Logo/naziv aplikacije (opciono)
   - Ime korisnika
   - Logout ikonu (umesto dugmeta)
3. ✅ Stilizuj kao tanku belu traku
4. ✅ Dodaj donji border `#00a1ff` (3px solid)
5. ✅ Podesi visinu (50px)

### **Faza 4: Layout Integracija**
1. ✅ Modifikuj `app.component.html` da include novi sidebar
2. ✅ Podesi CSS layout:
   - Header sticky top
   - Sidebar fixed left
   - Main content sa odgovarajućim padding-om (da ne prekriva sidebar)
3. ✅ Testiraj responzivnost

### **Faza 5: Testiranje i Prilagođavanje**
1. ✅ Testiranje funkcionalnosti navigacije
2. ✅ Provera hover efekata
3. ✅ Provera active states
4. ✅ Provera da sidebar ne prekriva content
5. ✅ Fine-tuning animacija i boja
6. ✅ Cross-browser testiranje

---

## 📝 CSS Animacije

### Sidebar Collapse/Expand
```css
.sidebar {
  width: 250px;
  transition: width 0.3s ease-in-out;
}

.sidebar.collapsed {
  width: 70px;
}

.sidebar .nav-text {
  opacity: 1;
  transition: opacity 0.2s ease;
}

.sidebar.collapsed .nav-text {
  opacity: 0;
  display: none;
}
```

### Hover Efekat
```css
.nav-item:hover {
  background: rgba(255, 255, 255, 0.1);
  border-right: 3px solid #f5f5f5;
}
```

---

## ⚠️ Potencijalni Problemi i Rešenja

### Problem 1: Sidebar prekriva content
**Rešenje**: Dodaj padding-left na main content koji odgovara širini sidebar-a

### Problem 2: Animacije nisu smooth
**Rešenje**: Koristi CSS transitions i will-change property

### Problem 3: Ikone se ne učitavaju
**Rešenje**: Proveri da su Material Icons pravilno uključeni u index.html

### Problem 4: Active route highlighting ne radi
**Rešenje**: Koristi Angular's `routerLinkActive` directive

### Problem 5: Responzivnost na malim ekranima
**Rešenje**: Na mobilnim uređajima sidebar može biti overlay ili hamburger menu

---

## 📦 Dodatne Opcije (za kasnije)

1. **Animirane tranzicije** između strana
2. **Tooltips** na collapsed ikonama
3. **Badge notifikacije** na linkovima
4. **Nested menije** (ako bude potrebno)
5. **Dark/Light mode toggle**
6. **Keyboard shortcuts** za navigaciju

---

## 🚀 Ready to Start?

Nakon pregleda ovog plana, možemo krenuti sa implementacijom korak po korak! 

**Predloženi redosled:**
1. Prvo testiramo osnovnu strukturu (samo HTML bez stilova)
2. Zatim dodajemo CSS
3. Na kraju fine-tuning i polish

Šta kažeš? Idemo redom ili želiš nešto da prilagodimo u planu?

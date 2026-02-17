# Dashboard Implementacija - Plan

## Pregled
Implementacija Dashboard stranice kao početne strane aplikacije sa statistikama i grafikonima za ulogovanog korisnika.

## Funkcionalnosti

### Faza 1: Osnovne Metrike (Početna Implementacija)
1. **Veliki Kartoni (Cards)**
   - Ukupan broj aktivnih projekata (za ulogovanog korisnika)
   - Ukupan broj nezavršenih aktivnosti (za ulogovanog korisnika)

2. **Grafikoni i Vizuelizacije**
   - Grafikon raspodele projekata po statusu (Pie/Donut chart)
   - Grafikon aktivnosti po mesecima (Bar chart - poslednjih 6 meseci)
   - Histogram aktivnosti po statusu (Bar chart)
   - Trend projekata - dodavanje novih projekata tokom vremena (Line chart - poslednjih 12 meseci)

### Faza 2: Dodatne Metrike (Buduće Proširenje)
- Top 5 projekata sa najviše aktivnosti
- Statistika završenih vs. nezavršenih aktivnosti po projektima
- Heatmap calendar aktivnosti
- Statistika devops task-ova

## Tehnički Stack

### Frontend
- **Framework**: Angular 18 (standalone komponente)
- **Grafička biblioteka**: **ngx-charts** (@swimlane/ngx-charts)
  - Razlog: Besplatna, bazirana na D3.js, moderne animacije, dobra integracija sa Angular
  - Alternativa: primeng charts (Chart.js wrapper) ili ApexCharts
- **UI Komponente**: PrimeNG (već u projektu)
- **Ikone**: Material Icons (već u projektu)

### Backend
- **Framework**: .NET Core Web API
- **Novi endpoint**: `DashboardController`
- **Servisi**: 
  - `DashboardService` - agregacija podataka
  - Korišćenje postojećih repozitorija (Projekti, Aktivnosti)

## Implementacija

### 1. Backend (API)

#### 1.1 Model za Dashboard statistiku
```csharp
// Models/DashboardStats.cs
public class DashboardStats
{
    public int ActiveProjectsCount { get; set; }
    public int UnfinishedActivitiesCount { get; set; }
    public List<ProjectStatusCount> ProjectsByStatus { get; set; }
    public List<MonthlyActivityCount> ActivitiesByMonth { get; set; }
    public List<ActivityStatusCount> ActivitiesByStatus { get; set; }
    public List<MonthlyProjectCount> NewProjectsByMonth { get; set; }
}

public class ProjectStatusCount
{
    public string Status { get; set; }
    public int Count { get; set; }
}

public class MonthlyActivityCount
{
    public string Month { get; set; }
    public int Year { get; set; }
    public int MonthNum { get; set; }
    public int Count { get; set; }
}

public class ActivityStatusCount
{
    public string Status { get; set; }
    public int Count { get; set; }
}

public class MonthlyProjectCount
{
    public string Month { get; set; }
    public int Year { get; set; }
    public int MonthNum { get; set; }
    public int Count { get; set; }
}
```

#### 1.2 DashboardController
```csharp
// Controllers/DashboardController.cs
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DashboardController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStats([FromQuery] int userId)
    {
        // NAPOMENA: U produkciji, userId treba dobiti iz Auth0 tokena
        // umesto da se šalje kao query parametar (security!)
        // Primer: var userId = GetUserIdFromToken();
        
        // Poziv stored procedure sp_GetDashboardStats
        // Vraća sve statistike odjednom (5 result setova)
    }
}
```

#### 1.3 Database - Stored Procedures
**Fajl**: `database/24_CreateDashboardProcedures.sql`

##### Glavna Procedura: sp_GetDashboardStats
Ova procedura vraća **5 result setova** u jednom pozivu za maksimalnu performansu:

```sql
EXEC sp_GetDashboardStats @UserId = 1;
```

**Result Set 1 - Osnovne Metrike:**
- `ActiveProjectsCount` - Broj aktivnih projekata
- `UnfinishedActivitiesCount` - Broj nezavršenih aktivnosti

**Result Set 2 - Projekti po Statusu:**
- `Status` - Naziv statusa projekta
- `Count` - Broj projekata sa tim statusom

**Result Set 3 - Aktivnosti po Mesecima:**
- `Month` - Format "MMM yyyy" (npr. "Feb 2026")
- `Year`, `MonthNum` - Za sortiranje
- `Count` - Broj aktivnosti u tom mesecu
- Poslednjih 6 meseci

**Result Set 4 - Aktivnosti po Statusu:**
- `Status` - Status aktivnosti ("U toku", "Planirano", "Završeno")
- `Count` - Broj aktivnosti sa tim statusom
- Sortirano po prioritetu statusa

**Result Set 5 - Novi Projekti po Mesecima:**
- `Month` - Format "MMM yyyy"
- `Year`, `MonthNum` - Za sortiranje
- `Count` - Broj novih projekata
- Poslednjih 12 meseci

##### Dodatne Procedure (pojedinačne):
- `sp_GetActiveProjectsCount` - Samo broj aktivnih projekata
- `sp_GetUnfinishedActivitiesCount` - Samo broj nezavršenih aktivnosti
- `sp_GetProjectsByStatus` - Samo raspodela projekata po statusu
- `sp_GetActivitiesByMonth` - Aktivnosti po mesecima (parametar: @MonthsBack)
- `sp_GetActivitiesByStatus` - Aktivnosti po statusu
- `sp_GetNewProjectsByMonth` - Novi projekti (parametar: @MonthsBack)
- `sp_GetTopProjectsByActivityCount` - Top N projekata (parametar: @TopCount)

##### Bezbednost i Permisije:
Sve procedure automatski filtriraju podatke tako da korisnik vidi samo:
- Projekte koje je **kreirao** (`Projekti.CreatedBy = @UserId`)
- Projekte na kojima ima **permisiju** (preko `ProjectPermissions` tabele)

```sql
WHERE (p.CreatedBy = @UserId 
       OR EXISTS (
           SELECT 1 FROM ProjectPermissions pp 
           WHERE pp.ProjekatId = p.Id AND pp.UserId = @UserId
       ))
```

##### Performanse:
Kreirani su dodatni indeksi za optimizaciju:
- `IX_Projekti_CreatedBy_Aktivan` - Za brže filtriranje aktivnih projekata
- `IX_Aktivnosti_Status_Datum` - Za brže grupisanje po statusu i datumu
- `IX_ProjectPermissions_UserId_ProjekatId` - Za brže provere permisija

#### 1.4 Implementacija Backend Logike

##### Poziv Stored Procedure u C#
```csharp
// Controllers/DashboardController.cs
[HttpGet("stats")]
public async Task<ActionResult<DashboardStats>> GetStats([FromQuery] int userId)
{
    var stats = new DashboardStats
    {
        ProjectsByStatus = new List<ProjectStatusCount>(),
        ActivitiesByMonth = new List<MonthlyActivityCount>(),
        ActivitiesByStatus = new List<ActivityStatusCount>(),
        NewProjectsByMonth = new List<MonthlyProjectCount>()
    };

    using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    {
        await connection.OpenAsync();

        using (var command = new SqlCommand("sp_GetDashboardStats", connection))
        {
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@UserId", userId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                // Result Set 1: Osnovne metrike
                if (await reader.ReadAsync())
                {
                    stats.ActiveProjectsCount = reader.GetInt32(0);
                    stats.UnfinishedActivitiesCount = reader.GetInt32(1);
                }

                // Result Set 2: Projekti po statusu
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.ProjectsByStatus.Add(new ProjectStatusCount
                        {
                            Status = reader.GetString(0),
                            Count = reader.GetInt32(1)
                        });
                    }
                }

                // Result Set 3: Aktivnosti po mesecima
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.ActivitiesByMonth.Add(new MonthlyActivityCount
                        {
                            Month = reader.GetString(0),
                            Year = reader.GetInt32(1),
                            MonthNum = reader.GetInt32(2),
                            Count = reader.GetInt32(3)
                        });
                    }
                }

                // Result Set 4: Aktivnosti po statusu
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.ActivitiesByStatus.Add(new ActivityStatusCount
                        {
                            Status = reader.GetString(0),
                            Count = reader.GetInt32(1)
                        });
                    }
                }

                // Result Set 5: Novi projekti po mesecima
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.NewProjectsByMonth.Add(new MonthlyProjectCount
                        {
                            Month = reader.GetString(0),
                            Year = reader.GetInt32(1),
                            MonthNum = reader.GetInt32(2),
                            Count = reader.GetInt32(3)
                        });
                    }
                }
            }
        }
    }

    return Ok(stats);
}
```

##### Alternativa: Korišćenje Dapper-a (ako je u projektu)
```csharp
[HttpGet("stats")]
public async Task<ActionResult<DashboardStats>> GetStats([FromQuery] int userId)
{
    using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    {
        var multi = await connection.QueryMultipleAsync(
            "sp_GetDashboardStats", 
            new { UserId = userId }, 
            commandType: CommandType.StoredProcedure
        );

        var stats = new DashboardStats();
        
        // Read result set 1
        var basicStats = await multi.ReadFirstAsync<dynamic>();
        stats.ActiveProjectsCount = basicStats.ActiveProjectsCount;
        stats.UnfinishedActivitiesCount = basicStats.UnfinishedActivitiesCount;
        
        // Read result set 2
        stats.ProjectsByStatus = (await multi.ReadAsync<ProjectStatusCount>()).ToList();
        
        // Read result set 3
        stats.ActivitiesByMonth = (await multi.ReadAsync<MonthlyActivityCount>()).ToList();
        
        // Read result set 4
        stats.ActivitiesByStatus = (await multi.ReadAsync<ActivityStatusCount>()).ToList();
        
        // Read result set 5
        stats.NewProjectsByMonth = (await multi.ReadAsync<MonthlyProjectCount>()).ToList();

        return Ok(stats);
    }
}
```

### 2. Frontend (Angular)

#### 2.1 Instalacija biblioteka
```bash
npm install @swimlane/ngx-charts --save
npm install @angular/cdk --save
```

#### 2.2 Nova komponenta: Dashboard
```
frontend/src/app/components/dashboard/
├── dashboard.component.ts
├── dashboard.component.html
├── dashboard.component.css
└── dashboard.component.spec.ts
```

#### 2.3 Service za Dashboard
```typescript
// services/dashboard.service.ts
@Injectable({ providedIn: 'root' })
export class DashboardService {
  getStats(userId: string): Observable<DashboardStats> {}
}
```

#### 2.4 Routing Update
- Promena default route sa `/projekti` na `/dashboard`
- Dodavanje nove rute za dashboard
```typescript
{ path: '', redirectTo: '/dashboard', pathMatch: 'full' },
{ path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] }
```

#### 2.5 Sidebar Update
- Dodavanje Dashboard linka kao prvog u navigaciji
- Ikona: `dashboard` ili `analytics`

### 3. UI/UX Dizajn

#### Layout
```
┌──────────────────────────────────────────────────┐
│  DASHBOARD HEADER                                │
├────────────────┬─────────────────────────────────┤
│                │                                 │
│  Aktivni       │  Nezavršene Aktivnosti         │
│  Projekti      │                                 │
│     [42]       │         [127]                   │
│                │                                 │
├────────────────┴─────────────────────────────────┤
│                                                  │
│  Projekti po Statusu (Pie Chart)                │
│                                                  │
├──────────────────────────────────────────────────┤
│                                                  │
│  Aktivnosti po Mesecima (Bar Chart)             │
│                                                  │
├──────────────────────────────────────────────────┤
│  Aktivnosti po       │  Novi Projekti po        │
│  Statusu             │  Mesecima                │
│  (Bar Chart)         │  (Line Chart)            │
│                      │                          │
└──────────────────────┴──────────────────────────┘
```

#### Paleta Boja
- Aktivni projekti karton: **#3B82F6** (plava)
- Nezavršene aktivnosti karton: **#EF4444** (crvena)
- Grafikoni: Koristiti ngx-charts ugrađene palete ili custom:
  - `['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899']`

## Koraci Implementacije

### Korak 1: Database Setup
- [x] Kreirati `database/24_CreateDashboardProcedures.sql`
- [x] Izvršiti SQL skriptu za kreiranje stored procedura
- [ ] Testirati stored procedure direktno (SQL Server Management Studio / Azure Data Studio)

### Korak 2: Backend Setup
- [ ] Kreirati `Models/DashboardStats.cs` sa svim potrebnim modelima
  - `DashboardStats`, `ProjectStatusCount`, `MonthlyActivityCount`, `ActivityStatusCount`, `MonthlyProjectCount`
- [ ] Kreirati `Controllers/DashboardController.cs`
- [ ] Implementirati poziv stored procedure `sp_GetDashboardStats`
- [ ] Parsirati 5 result setova u DashboardStats model (koristiti ADO.NET ili Dapper)
- [ ] Dodati connection string u appsettings.json (ako već ne postoji)
- [ ] Testirati API endpoint (Postman/HTTP file)

### Korak 3: Frontend Setup
- [ ] Instalirati ngx-charts biblioteku (`npm install @swimlane/ngx-charts`)
- [ ] Instalirati @angular/cdk (`npm install @angular/cdk`)
- [ ] Kreirati `dashboard` komponentu
- [ ] Kreirati `DashboardService`
- [ ] Dodati ngx-charts u app.config.ts (provideCharts)

### Korak 4: UI Implementacija
- [ ] Implementirati kartone sa osnovnim metrikama (ActiveProjectsCount, UnfinishedActivitiesCount)
- [ ] Implementirati Pie Chart za projekte po statusu
- [ ] Implementirati Bar Chart za aktivnosti po mesecima (6 meseci)
- [ ] Implementirati Bar Chart za aktivnosti po statusu (umesto prioriteta)
- [ ] Implementirati Line Chart za nove projekte (12 meseci)
- [ ] Stilizovati komponentu (responsive design, grid layout)

### Korak 5: Integracija
- [ ] Dodati Dashboard rutu u `app.routes.ts`
- [ ] Promeniti default rutu sa `/projekti` na `/dashboard`
- [ ] Dodati Dashboard link u sidebar (prvi item, ikona: dashboard)
- [ ] Testirati navigaciju iAuthGuard

### Korak 6: Poliranje
- [ ] Loading state (skeletoni ili spinneri)
- [ ] Error handling
- [ ] Responsive design za mobilne uređaje
- [ ] Animacije i transition efekti
- [ ] Accessibility (ARIA labels)

## API Endpoints

### GET /api/dashboard/stats?userId={userId}
**Response:**
```json
{
  "activeProjectsCount": 42,
  "unfinishedActivitiesCount": 127,
  "projectsByStatus": [
    { "status": "Aktivan", "count": 25 },
    { "status": "U pripremi", "count": 10 },
    { "status": "Završen", "count": 7 }
  ],
  "activitiesByMonth": [
    { "month": "jan 2026", "year": 2026, "monthNum": 1, "count": 15 },
    { "month": "feb 2026", "year": 2026, "monthNum": 2, "count": 23 }
  ],
  "activitiesByStatus": [
    { "status": "U toku", "count": 45 },
    { "status": "Planirano", "count": 62 },
    { "status": "Završeno", "count": 20 }
  ],
  "newProjectsByMonth": [
    { "month": "okt 2025", "year": 2025, "monthNum": 10, "count": 5 },
    { "month": "nov 2025", "year": 2025, "monthNum": 11, "count": 8 },
    { "month": "dec 2025", "year": 2025, "monthNum": 12, "count": 3 },
    { "month": "jan 2026", "year": 2026, "monthNum": 1, "count": 7 }
  ]
}
```

## Performanse i Optimizacija

### Backend
- **Stored Procedures** - sve queries su u stored procedurama za bolje performanse
- **Indeksi** - Kreirani optimizovani indeksi (vidi 24_CreateDashboardProcedures.sql):
  - `IX_Projekti_CreatedBy_Aktivan` - Za filtriranje aktivnih projekata po korisniku
  - `IX_Aktivnosti_Status_Datum` - Za grupisanje aktivnosti po statusu i datumu
  - `IX_ProjectPermissions_UserId_ProjekatId` - Za brže provere permisija
- **Jedan poziv baze** - `sp_GetDashboardStats` vraća sve podatke odjednom (5 result setova)
- **Cachiranje** (opciono) - Razmotriti cachiranje dashboard statistika (npr. 5 minuta) ako je potrebno
- **User Filtering** - Procedure automatski filtriraju samo podatke korisnika (bezbednost + performanse)

### Frontend
- Lazy loading grafikona (prikazati kartone prvo)
- Debounce na refresh dugme (ako ga dodamo)
- OnPush change detection strategija
- Virtual scrolling za velike liste (buduće proširenje)

## Testiranje

### Backend Tests
- Unit testovi za DashboardService
- Integration testovi za DashboardController
- Testiranje različitih user permisions (da li vidi samo svoje podatke)

### Frontend Tests
- Unit testovi za DashboardComponent
- Unit testovi za DashboardService
- E2E test - otvaranje dashboard stranice
- E2E test - učitavanje podataka

## Buduća Proširenja (v2)
- [ ] Filter po datumu (poslednji mesec, kvartal, godina)
- [ ] Export dashboard-a u PDF
- [ ] Personalizacija dashboard-a (drag & drop widgeta)
- [ ] Real-time updates (SignalR)
- [ ] Poređenje sa prethodnim periodom (trend strelice ↑↓)
- [ ] Notifikacije za kritične metrike

## Reference i Dokumentacija

### Database
- [24_CreateDashboardProcedures.sql](database/24_CreateDashboardProcedures.sql) - Stored Procedures za Dashboard

### Frontend Libraries
- ngx-charts: https://swimlane.github.io/ngx-charts/
- ngx-charts GitHub: https://github.com/swimlane/ngx-charts
- PrimeNG: https://primeng.org/
- Angular Material CDK: https://material.angular.io/cdk/
- FullCalendar (već u projektu): https://fullcalendar.io/

### Backend
- ADO.NET Documentation: https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/
- Dapper (ako se koristi): https://github.com/DapperLib/Dapper
- SQL Server Stored Procedures: https://learn.microsoft.com/en-us/sql/relational-databases/stored-procedures/stored-procedures-database-engine

---

**Verzija**: 1.1  
**Datum kreiranja**: 16.02.2026  
**Datum ažuriranja**: 16.02.2026 (dodane stored procedures)  
**Autor**: Dashboard Team

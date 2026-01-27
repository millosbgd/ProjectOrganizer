# DevOps Tasks Workflow - Dokumentacija

## Pregled

Novi workflow za generisanje DevOps taskova sa pregledom i selektovanjem pre čuvanja u bazu.

## Problem koji rešava

**Stari workflow:**
- Korisnik klikne "📋 DevOps Taskovi"
- AI generiše taskove
- Taskovi se **automatski upisuju u bazu**
- Problem: Ako korisnik klikne više puta, kreiraju se **duplikati**

**Novi workflow:**
- Korisnik klikne "📋 DevOps Taskovi"
- AI generiše taskove (prikazuju se u tekstualnom formatu)
- Korisnik klikne "📥 Pregledaj i sačuvaj"
- Otvara se preview modal sa parsiranim taskovima
- Korisnik **selektuje** koje taskove želi da sačuva (checkbox-ovi)
- Korisnik klikne "Sačuvaj selektovane"
- **Samo selektovani taskovi se upisuju u bazu**

## Backend Endpointi

### 1. Generate DevOps Tasks (Izmenjeno)
```
POST /api/aktivnosti/{id}/generate-devops-tasks
```
- **ŠTA RADI:** Poziva OpenAI API i generiše taskove u tekstualnom formatu
- **ŠTA NE RADI:** NE parsira, NE upisuje u bazu
- **Response:** `string` (raw AI output)

### 2. Parse DevOps Tasks (Novi)
```
POST /api/aktivnosti/{id}/parse-devops-tasks
Body: { "tasksText": "..." }
```
- **ŠTA RADI:** Parsira AI tekst u strukturirane objekte
- **ŠTA NE RADI:** NE upisuje u bazu
- **Response:** `Array<ParsedTask>` (JSON array taskova)

### 3. Save Selected Tasks (Novi)
```
POST /api/aktivnosti/{id}/save-selected-tasks
Body: { 
  "tasks": [
    {
      "title": "...",
      "description": "...",
      "acceptanceCriteria": "...",
      "priority": "High/Medium/Low",
      "estimation": "...",
      "orderIndex": 1
    }
  ]
}
```
- **ŠTA RADI:** Upisuje samo selektovane taskove u DevOpsTasksCandidates tabelu
- **Response:** `{ "message": "...", "count": 5 }`

## Frontend Komponente

### 1. ZapisnikModalComponent (Ažurirana)
- Prikazuje AI generisani tekst
- Dodato dugme **"📥 Pregledaj i sačuvaj"** (vidljivo samo za DevOps taskove)
- Event: `(preview)` - emituje se kada korisnik klikne na dugme

### 2. DevOpsTasksPreviewModalComponent (Nova)
**Fajlovi:**
- `devops-tasks-preview-modal.component.ts`
- `devops-tasks-preview-modal.component.html`
- `devops-tasks-preview-modal.component.css`

**Props:**
- `@Input() tasks: ParsedTask[]` - Parsirani taskovi
- `@Input() aktivnostId: number` - ID aktivnosti
- `@Output() close` - Zatvori modal
- `@Output() save` - Sačuvaj selektovane taskove

**Features:**
- Grid layout sa karticama taskova
- Checkbox za svaki task (default: svi selektovani)
- "Selektuj sve" checkbox
- Prikaz broja selektovanih taskova
- Badge-ovi za prioritet i procenu
- Dugmad: "Odustani" i "Sačuvaj selektovane (N)"

### 3. AktivnostModalComponent (Ažurirana)
**Nove metode:**
- `openPreview()` - Poziva parse endpoint i otvara preview modal
- `saveSelectedTasks(tasks)` - Poziva save endpoint sa selektovanim taskovima
- `closePreviewModal()` - Zatvara preview modal

**Nova stanja:**
- `showDevOpsTasksPreviewModal: boolean`
- `parsedTasks: ParsedTask[]`

## User Flow

```
1. Korisnik otvara aktivnost
   └──> Klikne "📋 DevOps Taskovi"
        └──> AI generiše taskove
             └──> Zapisnik modal prikazuje tekst
                  
2. U zapisnik modalu:
   └──> Korisnik vidi generisane taskove kao tekst
        └──> Klikne "📥 Pregledaj i sačuvaj"
             └──> Backend parsira tekst u JSON
                  └──> Preview modal se otvara

3. U preview modalu:
   └──> Korisnik vidi grid sa parsiranim taskovima
        └──> Svi taskovi su default selektovani ✅
             └──> Korisnik može deselektovati neželjene
                  └──> Klikne "Sačuvaj selektovane (N)"
                       └──> Backend upisuje samo selektovane
                            └──> Alert: "✅ Uspešno sačuvano N taskova!"
                                 └──> Modali se zatvaraju

4. Alternativni flow:
   └──> Korisnik klikne "Odustani" u preview modalu
        └──> Modal se zatvara bez upisivanja
             └──> Može ponovo kliknuti "DevOps Taskovi" i generisati nove
```

## Prednosti novog workflow-a

✅ **Bez duplih taskova** - Nema automatskog upisivanja  
✅ **Kontrola korisnika** - Eksplicitno selektovanje taskova  
✅ **Preview pre commita** - Korisnik vidi šta će biti sačuvano  
✅ **Fleksibilnost** - Može deselektovati neželjene taskove  
✅ **Transparentnost** - Jasna vizualizacija sa brojem selektovanih  

## Tehnički detalji

### Parsing logika
```csharp
ParseDevOpsTasksToObjects(string tasksText)
```
- Splituje po markeru: `[TASK N]`
- Ekstraktuje polja: Naziv, Opis, Acceptance Criteria, Prioritet, Procena
- Koristi regex pattern matching
- Helper metoda: `ExtractField(section, pattern, singleLine)`

### DTO klase
```csharp
public class ParseTasksRequest
{
    public string TasksText { get; set; }
}

public class SaveTasksRequest
{
    public List<TaskDto> Tasks { get; set; }
}

public class TaskDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string AcceptanceCriteria { get; set; }
    public string Priority { get; set; }
    public string Estimation { get; set; }
    public int OrderIndex { get; set; }
}
```

### Frontend interface
```typescript
export interface ParsedTask {
  orderIndex: number;
  title: string;
  description?: string;
  acceptanceCriteria?: string;
  priority?: string;
  estimation?: string;
  selected?: boolean; // Za checkbox tracking
}
```

## Deployment

### Backend
```bash
cd backend/ProjectOrganizer.Api
dotnet build
dotnet publish -c Release -o ./publish
cd publish
Compress-Archive -Path * -DestinationPath deploy.zip -Force
az webapp deploy --resource-group rg-projectorganizer-dev --name ProjectOrganizer --src-path deploy.zip
```

### Frontend
```bash
git add .
git commit -m "Add DevOps tasks preview and selection workflow"
git push origin deploy
# Static Web App automatski deploya sa GitHub-a
```

## Testiranje

1. **Test 1: Generisanje taskova**
   - Otvori aktivnost
   - Klikni "📋 DevOps Taskovi"
   - Proveri da se pojavi loader "AI generiše taskove..."
   - Proveri da se prikaže AI tekst u zapisnik modalu

2. **Test 2: Preview modal**
   - Klikni "📥 Pregledaj i sačuvaj"
   - Proveri da se otvori preview modal
   - Proveri da su svi taskovi prikazani u grid-u
   - Proveri da su svi default selektovani

3. **Test 3: Selekcija**
   - Deselektuj neki task
   - Proveri da se brač selektovanih ažurira
   - Klikni "Selektuj sve"
   - Proveri da svi postanu selektovani

4. **Test 4: Čuvanje**
   - Selektuj samo 2 od 5 taskova
   - Klikni "Sačuvaj selektovane (2)"
   - Proveri alert: "✅ Uspešno sačuvano 2 taskova!"
   - Otvori "👁️ Pregledaj" - proveri da ima tačno 2 taska

5. **Test 5: Prevencija duplih**
   - Ponovo klikni "📋 DevOps Taskovi"
   - AI ponovo generiše taskove
   - NE klikni "Pregledaj i sačuvaj" - samo zatvori
   - Otvori "👁️ Pregledaj" - proveri da NEMA novih taskova

## Budući planovi

- [ ] Dodati detekciju duplih taskova u preview modalu
- [ ] Omogućiti editovanje taskova pre čuvanja
- [ ] Dodati bulk edit (npr. svi taskovi = High priority)
- [ ] Integracija sa Azure DevOps API (slanje taskova)
- [ ] History verzionisanje (koji taskovi su bili generisani kada)

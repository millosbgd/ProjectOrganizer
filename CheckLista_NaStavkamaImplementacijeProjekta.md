Proširujemo funkcionalnost praćenja implemantacije na projektima.

Na stavci implementacije na projektu (ProjectImplementationItems) dodeljujemo stavke CheckListe koje su definisane u izabranom modelu.

Već postoji funkcionalnost kreiranja stavki implementacije za konkretan projekat prilikom izbora modela implemantacje na projektu, sada dodajemo i kreiranje stavki check liste sa svaku krieranu stavku implementacije u istom tom tom trenutku.

Treba kreirati novu tabelu gde ćemo pisati vezi izmedju stavke implementacije projekta i Id stavke check liste (iz tabele ImplementationItemCheckListItems). U ovoj tabeli predvidi i bit polje "Završeno".

Prikaz podataka iz ove tabele obezbediti na formi za ažuriranje stavke implementacije (grid u donjem delu).

Takođe, dodati kontrolu da na stavki implementacije ne može da se kliken na Završeno i potvrđeno od strane klijenta dok sve stavke ček liste ne budu setovane na Zavrseno = true.

---

## Implementacija (15. Februar 2026)

### Backend

**1. Novi Model: ProjectImplementationItemCheckList.cs**
- Kreirana nova tabela za praćenje statusa čeklisti za stavke implementacije projekta
- Polja: Id, ProjectImplementationItemId, CheckListItemId, Zavrsen (BIT), ZavrsenDatum (DATETIME)
- Navigation properties ka ProjectImplementationItem i CheckListItem

**2. ApplicationDbContext.cs**
- Dodat DbSet<ProjectImplementationItemCheckList>
- Konfigurisan relationship: CASCADE delete za ProjectImplementationItem, NO ACTION za CheckListItem
- Kreirani indeksi za optimalne query performanse

**3. ProjektiController.cs - CreateProjectImplementationItems**
- Dodata logika za automatsko kreiranje čeklisti prilikom kreiranja stavki implementacije projekta
- Kada se izabere model implementacije, sistem kopira sve čeklistu stavke iz ImplementationItemCheckListItems u ProjectImplementationItemCheckLists
- Svaka stavka implementacije projekta dobija svoje instance čeklista sa Zavrsen=false

**4. ProjectImplementationItemsController.cs**
- **GetByProjectId**: Proširena sa Include za CheckLists i vraća kompletan podatak sa checkListItemOpis
- **GetById** (novi endpoint): Vraća detalje pojedinačne stavke sa svim čeklistama
- **UpdateCheckList** (novi endpoint): `PUT api/ProjectImplementationItems/{itemId}/checklists/{checkListId}`
  - Toggle Zavrsen statusa
  - Automatski postavlja ZavrsenDatum kada se označi kao završeno
- **Update**: Dodata validacija - ne dozvoljava postavljanje Zavrseno=true ili KlijentPotvrdio=true ako nisu sve čekliste završene

### Database

**Migration 23_CreateProjectImplementationItemCheckLists.sql**
- Kreirana tabela ProjectImplementationItemCheckLists sa svim FK constraints
- Kreirani indeksi:
  - IX_ProjectImplementationItemCheckLists_ProjectImplementationItemId
  - IX_ProjectImplementationItemCheckLists_CheckListItemId
  - IX_ProjectImplementationItemCheckLists_ProjectItem_Zavrsen (composite)

### Frontend

**1. Models (project-implementation-item.model.ts)**
- Dodat property `checkLists?: ProjectImplementationCheckListItem[]` u ProjectImplementationItem interface
- Kreiran novi interface `ProjectImplementationCheckListItem`:
  - id, checkListItemId, checkListItemOpis, zavrsen, zavrsenDatum

**2. Services (project-implementation-item.service.ts)**
- **getById(id: number)**: Učitava kompletne detalje stavke sa čeklistama
- **updateCheckList(itemId, checklistId, zavrsen)**: PUT endpoint za toggle checkbox-a

**3. Components (implementation-item-modal)**
- **TypeScript (implementation-item-modal.component.ts)**:
  - Injektovan ProjectImplementationItemService
  - `allCheckListsCompleted` getter - proverava da li su sve čekliste završene
  - `onCheckListToggle()` - poziva service.updateCheckList i ažurira UI
  - `onZavrsenoChange()` i `onKlijentPotvrdioChange()` - validacija sa alert porukom ako čekliste nisu završene
  - `openImplementationItemModal()` u parent komponenti (projekat-detail) - učitava punu stavku sa čeklistama pre otvaranja modala

- **HTML Template (implementation-item-modal.component.html)**:
  - Dodana čeklista sekcija između Napomena i Završeno checkbox-a
  - Tabela sa kolonama: ✓ checkbox, Opis stavke, Datum završetka
  - Checkbox toggle za svaku stavku čekliste
  - Prikaz datuma završetka kada je stavka označena

- **CSS (implementation-item-modal.component.css)**:
  - Stilovi za .checklist-container i .checklist-table
  - Hover efekti za redove tabele
  - Responsive dizajn za checkbox inpute

**4. Parent Component (projekat-detail.component.ts)**
- Modifikovan `openImplementationItemModal()` metod da koristi service.getById() umesto kopiranja objekta
- Sada učitava sve podatke uključujući čekliste pre otvaranja modala

### Testiranje

**Scenario testa:**
1. ✅ Kreirati novi projekat i izabrati model implementacije koji ima definisane čekliste
2. ✅ Verifikovati da su stavke implementacije kreirane sa svim čeklistama (Zavrsen=false)
3. ✅ Otvoriti edit modal za stavku implementacije
4. ✅ Proveriti da se prikazuje grid sa svim stavkama čekliste
5. ✅ Toggle checkbox-ove za pojedinačne stavke - verify API call i UI update
6. ✅ Pokušati označiti stavku kao Završeno bez finished checklists - verify validation alert
7. ✅ Završiti sve stavke čekliste
8. ✅ Sada označiti stavku kao Završeno i Klijent potvrdio - verify success

### Deployment

**Commit:** f9d66fb - "Fixed TypeScript errors in checklist template"  
**Datum:** 15. Februar 2026  
**Backend:** Deployed to https://projectorganizer.azurewebsites.net  
**Frontend:** Deployed to https://delightful-pebble-0fc0d9403.4.azurestaticapps.net via GitHub Actions  
**Database:** Migration 23 executed successfully on Azure SQL Database

### Napomene

- SQL Server ne podržava `RESTRICT` keyword - koristi se `NO ACTION` ili se izostavlja (default)
- TypeScript strict mode zahteva eksplicitne null checks u template-ima (`item && item.checkLists` umesto `item?.checkLists`)
- Backend vraća `checkListItemOpis` kao część projekcije, što omogućava jednostavan prikaz u UI
- Validacija na backend-u sprečava data integrity issues kada korisnik pokušava ručno pozvati API

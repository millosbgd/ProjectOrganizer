# Specifikacija funkcionalnosti: Kalendar aktivnosti

## Cilj

Implementirati prikaz i upravljanje aktivnostima kroz kalendarski prikaz korišćenjem FullCalendar biblioteke u Angular aplikaciji, uz .NET 8 Web API backend.

Funkcionalnost mora biti skalabilna i spremna za buduća proširenja (ponavljajuće aktivnosti, audit, konkurentnost podataka).

---

# Frontend zahtevi (Angular)

## Prikazi

- dayGridMonth (mesečni prikaz)
- timeGridWeek (nedeljni prikaz sa satnicom)
- timeGridDay (dnevni prikaz sa satnicom)

## Učitavanje podataka

- Aktivnosti se učitavaju dinamički po opsegu datuma.
- Koristiti endpoint:
  GET /api/activities?from=...&to=...
- Nikada ne učitavati sve aktivnosti odjednom.

## Interakcije

- Omogućiti:
  - Drag & Drop (promena datuma/vremena)
  - Resize (promena trajanja aktivnosti)

- Prilikom pomeranja ili izmene trajanja:
  - Pozvati PATCH /api/activities/{id}/time
  - Ako API vrati grešku, vratiti aktivnost na prethodnu vrednost (revert)

## Pravila rada sa datumima

- Datume slati backendu isključivo u UTC formatu (ISO string).
- U bazi se čuva UTC.
- U interfejsu se prikazuje lokalno vreme.

## Vizuelna pravila

- Boja aktivnosti zavisi od tipa aktivnosti.
- Greške iz API-ja moraju biti pravilno obrađene.
- Koristiti Angular servis sloj za komunikaciju sa API-jem.
- Auth0 token mora biti automatski dodat u zahteve.

---

# Backend zahtevi (.NET 8)

## Endpointi

### GET /api/activities?from&to

- Vraća aktivnosti koje se preklapaju sa zadatim opsegom.
- Filtrira po trenutno prijavljenom korisniku (OwnerSub iz JWT tokena).
- Vraća DTO prilagođen FullCalendar-u.

### PATCH /api/activities/{id}/time

- Ažurira StartUtc i EndUtc.
- Validira da je EndUtc > StartUtc.
- Proverava da aktivnost pripada prijavljenom korisniku.
- Vraća 204 NoContent pri uspehu.

---

# Poslovna pravila

- Svi datumi se čuvaju u UTC formatu.
- Validacija se uvek vrši na serverskoj strani.
- Samo vlasnik aktivnosti može da je menja.
- Definisati da li su preklapanja aktivnosti dozvoljena ili nisu.
- Aktivnosti ne smeju imati negativno trajanje.

---

# Performanse

- Obavezno dodati indekse u bazi na:
  - OwnerSub
  - StartUtc
  - EndUtc

- Query mora koristiti preklapanje opsega:
  (StartUtc < to && EndUtc > from)

---

# Buduća proširenja (Future-proofing)

## Ponavljajuće aktivnosti
- Planirati mogućnost uvođenja recurrence pravila (npr. RRULE).
- Ne vezivati implementaciju za jednokratne događaje.

## Soft Delete
- Razmotriti uvođenje IsDeleted polja.
- GET endpoint ne sme vraćati obrisane aktivnosti.

## Audit podaci
- CreatedUtc
- ModifiedUtc
- CreatedBy
- ModifiedBy

## Konkurentnost (Concurrency)
- Razmotriti korišćenje RowVersion (timestamp) kolone.
- Sprečiti overwriting podataka u slučaju paralelnih izmena.

---

# Rukovanje greškama

- Koristiti optimistički update u UI.
- U slučaju greške:
  - Vratiti prethodno stanje (revert).
  - Prikazati korisniku razumljivu poruku.
- Vraćati odgovarajuće HTTP status kodove (400, 401, 403, 404).

---

# Kvalitet koda

- Poštovati postojeću arhitekturu projekta.
- Koristiti async/await.
- Razdvojiti DTO, servis i kontroler slojeve.
- Kod mora biti produkcijski spreman.

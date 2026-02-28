# 🔒 Bezbednosna Poboljšanja - API Ključevi i Tokeni

## Pregled izmena

Implementirana su značajna bezbednosna poboljšanja za rukovanje osetljivim podacima kao što su OpenAI API ključevi i Azure DevOps Personal Access Tokens.

---

## 🚨 Problemi koji su rešeni

### Prije:
1. ❌ **API ključevi su vraćani u plain text-u** preko GET zahteva
2. ❌ **Ključevi bili vidljivi u:**
   - Network tab (browser dev tools)
   - Frontend komponenti (memorija)
   - Potencijalno u logovima
3. ❌ **Nema enkripcije u bazi** - ključevi čuvani kao plain text
4. ❌ **Over-posting ranjivost** - backend primao ceo model bez validacije

### Sada:
1. ✅ **Svi API ključevi enkriptovani** AES-256 algoritmom
2. ✅ **Backend nikad ne vraća pun ključ** - samo maskiranu verziju (npr. `sk-...****abcd`)
3. ✅ **DTO modeli** za siguran prenos podataka
4. ✅ **Validacija ulaznih podataka** (format OpenAI ključa, model izbor)
5. ✅ **Logovanje svih izmena** ključeva
6. ✅ **Mogućnost brisanja** ključeva

---

## 📁 Izmenjeni Fajlovi

### Backend:

#### Novi fajlovi:
- ✨ `Models/UserSettingsDto.cs` - DTO modeli za siguran prenos
- ✨ `Services/EncryptionService.cs` - AES-256 enkripcija/dekripcija

#### Ažurirani fajlovi:
- 🔧 `Program.cs` - Registracija `EncryptionService`
- 🔧 `Controllers/UserSettingsController.cs` - Koristi DTO i enkriptuje podatke
- 🔧 `Controllers/AktivnostiController.cs` - Dekriptuje ključeve pre upotrebe
- 🔧 `appsettings.json` - Dodana `Security:EncryptionKey` konfiguracija

### Frontend:

#### Ažurirani fajlovi:
- 🔧 `models/user-settings.model.ts` - Dodati `UserSettings` i `UpdateUserSettings` interfejsi
- 🔧 `services/user-settings.service.ts` - Koristi `UpdateUserSettings` za izmene
- 🔧 `components/settings/settings.component.ts` - Nova logika sa maskiranim ključevima
- 🔧 `components/settings/settings.component.html` - Novi UI za prikaz i izmenu
- 🔧 `components/settings/settings.component.css` - Stilovi za maskirane vrednosti

---

## 🔐 Kako radi Enkripcija

### 1. Čuvanje ključa:
```
Korisnički unos → Frontend
                    ↓
                API zahtev (HTTPS)
                    ↓
         Backend Controller
                    ↓
         EncryptionService.Encrypt()
                    ↓
         AES-256 enkripcija sa IV
                    ↓
         Base64 encoding
                    ↓
         Baza podataka (enkriptovano)
```

### 2. Prikazivanje ključa:
```
Baza podataka (enkriptovano)
                ↓
    EncryptionService.Decrypt()
                ↓
        Plain text (samo u memoriji)
                ↓
    EncryptionService.MaskValue()
                ↓
    DTO sa maskiranom vrednošću (npr. "sk-...****abcd")
                ↓
        Frontend prikaz
```

### 3. Korišćenje ključa (npr. OpenAI API):
```
Baza podataka (enkriptovano)
                ↓
    EncryptionService.Decrypt()
                ↓
        Plain text (samo u memoriji)
                ↓
    Pass to OpenAIService
                ↓
    OpenAI API call
```

---

## ⚙️ Konfiguracija

### 🔑 Encryption Key

**KRITIČNO ZA PRODUKCIJU:**

U `appsettings.json` dodajte jak encryption key:

```json
{
  "Security": {
    "EncryptionKey": "YOUR_STRONG_RANDOM_KEY_HERE_MinLength32Characters!"
  }
}
```

#### Generisanje sigurnog ključa (PowerShell):
```powershell
# Generisanje 32-bajtnog random stringa
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
Write-Host "Encryption Key: $key"
```

#### Ili jednostavno:
```powershell
# Generisanje random alfanumeričkog stringa (48 karaktera)
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 48 | % {[char]$_})
```

**VAŽNO:**
- ⚠️ **Ključ mora biti minimalno 32 karaktera**
- ⚠️ **Nikad ne commituj pravi ključ u Git**
- ⚠️ **Koristi Azure Key Vault ili Environment Variables u produkciji**
- ⚠️ **Ako promeniš ključ, svi postojeći enkriptovani podaci postaju neupotrebljivi**

### Azure App Service Configuration

Umesto hardkodiranja u `appsettings.json`, dodaj kao Application Setting:

```
Ime: Security__EncryptionKey
Vrednost: <generisani-key>
```

---

## 🎯 Frontend Izmene

### Novi Flow:

#### 1. Učitavanje podešavanja:
```typescript
// Backend vraća:
{
  "openAiApiKeyMasked": "sk-...****abcd",  // Maskirano
  "hasOpenAiApiKey": true,                  // Da li postoji
  "openAiModel": "gpt-4o-mini"
}
```

#### 2. Ažuriranje podešavanja:
```typescript
// Frontend šalje samo promene:
{
  "openAiApiKey": "sk-proj-new-key",  // Samo ako korisnik unese novi
  "openAiModel": "gpt-4o-mini"
}

// Ili briše ključ:
{
  "openAiApiKey": "REMOVE",
  "openAiModel": "gpt-4o-mini"
}
```

---

## 📊 Validacije

### Backend validacije:
- ✅ OpenAI ključ mora početi sa `sk-`
- ✅ Model mora biti jedan od: `gpt-4o-mini`, `gpt-4o`, `gpt-4-turbo`
- ✅ Maksimalna dužina ključa: 500 karaktera
- ✅ Samo autentifikovani korisnici mogu pristupiti

### Frontend validacije:
- ✅ Opcioni unos - prikaže trenutnu maskiran vrednost
- ✅ Clear field nakon uspešnog čuvanja
- ✅ Potvrda pre brisanja ključa

---

## 🔄 Migracija Postojećih Podataka

Ako u bazi već imaš plain text API ključeve, moraš ih enkriptovati:

### SQL Script za migraciju:

⚠️ **OVO NE MOŽE BITI SQL SKRIPTA** - mora se uraditi kroz C# kod jer zahteva EncryptionService.

### Ručna migracija:

1. Obriši postojeće plain text ključeve iz baze:
```sql
UPDATE UserSettings SET OpenAiApiKey = NULL, DevOpsPersonalAccessToken = NULL;
```

2. Korisnici moraju ponovo uneti svoje ključeve kroz UI

---

## 🧪 Testiranje

### Test encryption/decryption:
```csharp
// U nekom Controller endpoint-u za testing:
[HttpPost("test-encryption")]
public IActionResult TestEncryption([FromBody] string plainText)
{
    var encrypted = _encryptionService.Encrypt(plainText);
    var decrypted = _encryptionService.Decrypt(encrypted);
    var masked = _encryptionService.MaskOpenAiKey(decrypted);
    
    return Ok(new { 
        encrypted, 
        decrypted, 
        masked, 
        match = plainText == decrypted 
    });
}
```

---

## 📝 Best Practices

### ✅ DO:
- Uvek koristi HTTPS
- Čuvaj encryption key u Azure Key Vault
- Loguj pristupe osetljivim podacima
- Koristi različite ključeve zadev/staging/prod

### ❌ DON'T:
- Nikad ne loguj dekriptovane ključeve
- Ne šalji ključeve kroz URL parametre
- Ne cache-uj dekriptovane vrednosti na frontendu
- Ne commituj encryption key u Git

---

## 🚀 Deployment Checklist

Pre deploy-a na produkciju:

- [ ] Generiši jak encryption key
- [ ] Dodaj key kao Azure App Service Application Setting
- [ ] Ukloni default key iz appsettings.json
- [ ] Proveri da li HTTPS radi
- [ ] Obriši postojeće plain text ključeve iz baze
- [ ] Obavesti korisnike da ponovo unesu API ključeve
- [ ] Testiraj na staging okruženju
- [ ] Proveri logove - da li se leak-uju ključevi

---

## 🛡️ Security Considerations

### Šta je zaštićeno:
✅ Ključevi u bazi (AES-256 enkriptovani)  
✅ Ključevi u transport (HTTPS)  
✅ Ključevi u API response-u (maskirani)

### Šta NIJE zaštićeno:
⚠️ Ključevi u memoriji backend servera tokom korišćenja  
⚠️ Ključevi kada se šalju ka OpenAI/DevOps API-ima  
⚠️ Logovi ako se greška desi sa punim ključem

### Dodatna poboljšanja (opciono):
- Implementirati key rotation
- Dodati audit log za sve izmene ključeva
- Rate limiting na API endpoint-ima
- Multi-factor authentication za osetljive izmene

---

## 📞 Support

Ako imaš pitanja ili probleme:
1. Proveri da li encryption key postoji u konfiguraciji
2. Proveri logove backend-a
3. Proveri browser console za greške
4. Testiraj na lokalnom okruženju prvo

---

**Autor:** AI Assistant  
**Datum:** 28.02.2026  
**Verzija:** 1.0

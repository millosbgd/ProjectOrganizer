using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace ProjectOrganizer.Api.Services;

public record NotifyUserEntry(string Name, string Message);

public record ReminderExtraction(
    bool HasReminder,
    DateTime? RemindAt,
    string? Message,
    bool HasNotifyUser = false,
    List<NotifyUserEntry>? NotifyUsers = null);

public class OpenAIService
{
    public async Task<ReminderExtraction> ExtractReminderAsync(
        string apiKey,
        string model,
        string opis,
        string detalji,
        DateTime today)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ReminderExtraction(false, null, null);

        var text = $"{opis}\n{detalji}".Trim();
        if (string.IsNullOrWhiteSpace(text))
            return new ReminderExtraction(false, null, null);

        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(model);

        var systemPrompt = "Ti si asistent koji analizira tekst aktivnosti i ekstrahuje podsetnike i zahteve za obaveštavanje korisnika. Odgovaraš isključivo validnim JSON-om, bez ikakvog dodatnog teksta ili formatiranja.";

        var userPrompt = $$"""
            Trenutno tačno vreme je {{today:yyyy-MM-ddTHH:mm:ss}} (UTC).

            Analiziraj sledeći tekst i odredi da li postoji PODSETNIK i/ili OBAVEŠTENJE KORISNIKU.

            === PODSETNIK (hasReminder) ===
            Postavi hasReminder=true ako tekst sadrži BILO ŠTA od sledećeg:
            - eksplicitno traži podsetnik: "podseti me", "setiti me", "remind me", "podsetiti"
            - pominje da nešto TREBA da se uradi za određeno vreme: "za 10 minuta", "za sat vremena", "do 15h", "sutra ujutru", "sledećeg ponedeljka"
            - pominje rok ili deadline za neku akciju: "mora biti gotovo do", "treba poslati do", "rok je"
            - opisuje buduću akciju sa vremenskim okvirom: "pozvaću za", "treba upisati za", "biće gotovo za"
            Ako je hasReminder=true, popuni remindAt (ISO 8601 UTC, izračunaj od trenutnog vremena) i message (max 150 znakova).

            === OBAVEŠTENJE KORISNICIMA (hasNotifyUser) ===
            Postavi hasNotifyUser=true ako tekst EKSPLICITNO traži da se neko obavesti, npr:
            - "Obavesti Marka da..."
            - "Javi Ani o..."
            - "Reci Petru da..."
            - "Obavesti Milicu i Marka da..."
            Može biti JEDAN ILI VIŠE korisnika.
            Ako je hasNotifyUser=true, popuni notifyUsers kao NIZ objekata, svaki sa:
            - name: ime korisnika u NOMINATIVU (osnovna forma, npr. "Marko" ne "Marka", "Ana" ne "Ani", "Petar" ne "Petru")
            - message: šta treba da zna taj korisnik (max 200 znakova)

            Vrati ISKLJUČIVO validan JSON bez ikakvog dodatnog teksta. Primeri:
            Samo podsetnik: {"hasReminder": true, "remindAt": "2026-03-15T16:00:00", "message": "Poziv klijentu", "hasNotifyUser": false, "notifyUsers": []}
            Jedan korisnik: {"hasReminder": false, "hasNotifyUser": true, "notifyUsers": [{"name": "Marko", "message": "Treba da pregleda dokumentaciju"}]}
            Više korisnika: {"hasReminder": false, "hasNotifyUser": true, "notifyUsers": [{"name": "Milica", "message": "Sastanak sutra"}, {"name": "Marko", "message": "Sastanak sutra"}]}
            Oboje: {"hasReminder": true, "remindAt": "2026-03-15T16:00:00", "message": "Poziv", "hasNotifyUser": true, "notifyUsers": [{"name": "Marko", "message": "Prisustvuje pozivu"}]}
            Ništa: {"hasReminder": false, "hasNotifyUser": false, "notifyUsers": []}

            Tekst: {{text}}
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var response = await chatClient.CompleteChatAsync(messages);
        var raw = response.Value.Content[0].Text.Trim();

        // Ukloni markdown code block ako postoji
        if (raw.StartsWith("```"))
        {
            var end = raw.LastIndexOf("```");
            raw = raw[3..end].Trim();
            if (raw.StartsWith("json")) raw = raw[4..].Trim();
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        bool hasReminder = root.TryGetProperty("hasReminder", out var hasEl) && hasEl.GetBoolean();

        DateTime? remindAt = null;
        string? message = null;

        if (hasReminder)
        {
            if (root.TryGetProperty("remindAt", out var remindAtEl))
            {
                var parsed = DateTime.Parse(remindAtEl.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
                remindAt = parsed.Kind == DateTimeKind.Utc ? parsed : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }
            if (root.TryGetProperty("message", out var messageEl))
                message = messageEl.GetString();
        }

        bool hasNotifyUser = root.TryGetProperty("hasNotifyUser", out var hasNotifyEl) && hasNotifyEl.GetBoolean();
        var notifyUsers = new List<NotifyUserEntry>();

        if (hasNotifyUser && root.TryGetProperty("notifyUsers", out var notifyUsersEl)
            && notifyUsersEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in notifyUsersEl.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                var msg  = item.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
                if (!string.IsNullOrWhiteSpace(name))
                    notifyUsers.Add(new NotifyUserEntry(name, msg ?? "Imaš obaveštenje u aktivnosti"));
            }
        }

        return new ReminderExtraction(hasReminder, remindAt, message, hasNotifyUser || notifyUsers.Count > 0, notifyUsers);
    }

    public async Task<string> GenerateZapisnikAsync(
        string apiKey,
        string model,
        string klijentNaziv,
        string projekatNaziv,
        DateTime datum,
        string vrsta,
        string status,
        string opis,
        string detalji)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured for this user.");

        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(model);
        var systemPrompt = @"Ti si profesionalni asistent koji pomaže u pisanju formalnih zapisnika sa sastanaka i aktivnosti.
Tvoj zadatak je da transformišeš brzinske beleške u formalni email zapisnik.

Format email-a treba da bude:
- Profesionalan ton
- Jasna struktura sa sekcijama
- Bullet points za akcione stavke
- Zaključak sa next steps

Piši na srpskom jeziku (latinica).";

        var userPrompt = $@"Napravi formalni email zapisnik na osnovu sledećih informacija:

**Klijent:** {klijentNaziv}
**Projekat:** {projekatNaziv}
**Datum:** {datum:dd.MM.yyyy}
**Tip aktivnosti:** {vrsta}
**Status:** {status}
**Kratak opis:** {opis}

**Brzinske beleške:**
{detalji}

Generiši profesionalni email zapisnik koji mogu copy-paste i poslati. Email treba da ima:
- Subject liniju
- Pozdravni deo
- Sažetak sastanka/aktivnosti
- Detaljne tačke (bullet points)
- Akcione stavke (next steps)
- Zaključak

Obrati pažnju da tekst bude jasan, koncizan i profesionalan.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var response = await chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }

    public async Task<string> GenerateDevOpsTasks(
        string apiKey,
        string model,
        string klijentNaziv,
        string projekatNaziv,
        string opis,
        string detalji)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured for this user.");

        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(model);

        var systemPrompt = @"Ti si ekspert za projektni menadžment i Azure DevOps.
Tvoj zadatak je da analiziraš tekst sa aktivnosti/sastanka i extrahuješ konkretne akcione stavke koje treba realizovati.

Za svaki task koji identifikuješ, treba da napraviš strukturiran predlog u formatu za Azure DevOps.

Format taskova:
**[TASK {broj}]**
Naziv: {kratko, jasno ime taska}
Opis: {detaljniji opis šta treba uraditi}
Acceptance Criteria:
- {kriterijum 1}
- {kriterijum 2}
Prioritet: {High/Medium/Low}
Procena: {koliko sati/story points}

Piši na srpskom jeziku (latinica).";

        var userPrompt = $@"Analiziraj sledeći tekst i izvuci sve akcione stavke kao predlog za Azure DevOps taskove:

**Projekat:** {projekatNaziv}
**Klijent:** {klijentNaziv}
**Opis aktivnosti:** {opis}

**Detalji:**
{detalji}

Generiši strukturirane taskove spremne za kreiranje u Azure DevOps-u. Izdvoj samo konkretne akcije koje treba realizovati.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var response = await chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }

    public async Task<string> ExtractTextFromImageAsync(
        string apiKey,
        string model,
        string imageBase64)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured for this user.");

        if (string.IsNullOrWhiteSpace(imageBase64))
            throw new ArgumentException("Image data is required.");

        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(model);

        var systemPrompt = @"Ti si stručnjak za prepoznavanje teksta sa slika (OCR) sa posebnim fokusom na rukopis.

TVOJ ZADATAK:
- Pažljivo analiziraj sliku i pročitaj SVE vidljive reči i brojeve
- Posebnu pažnju obrati na RUKOPISNI tekst - pokušaj da prepoznaš svako slovo čak i kada je nejasno
- Očuvaj strukturu i formatiranje (novi redovi, liste, paragrafi)
- Ako neki deo teksta nije čitljiv, napiši [NEJASNO] ali nastavi sa ostatkom
- VAŽNO: Ne izmišljaj tekst - piši samo ono što vidiš na slici

FORMAT ODGOVORA:
- Čist tekst bez dodatnih komentara
- Samo prepoznat sadržaj sa slike";

        var userPrompt = "Analiziraj ovu sliku i pročitaj SVE vidljive tekstualne informacije. Posebno pažljivo pročitaj bilo kakav rukopisni tekst:";

        // Prepare image content
        var imageBytes = Convert.FromBase64String(imageBase64);
        var imageContentPart = ChatMessageContentPart.CreateImagePart(
            BinaryData.FromBytes(imageBytes),
            "image/png"  // or detect from base64 prefix
        );

        var textContentPart = ChatMessageContentPart.CreateTextPart(userPrompt);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(textContentPart, imageContentPart)
        };

        var response = await chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }

    /// <summary>
    /// Generic method to generate text using OpenAI with a custom prompt
    /// </summary>
    public async Task<string> GenerateTextAsync(string apiKey, string prompt, string model = "gpt-4o")
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured for this user.");

        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt is required.");

        var openAiClient = new OpenAIClient(apiKey);
        var chatClient = openAiClient.GetChatClient(model);

        var messages = new List<ChatMessage>
        {
            new UserChatMessage(prompt)
        };

        var response = await chatClient.CompleteChatAsync(messages);
        return response.Value.Content[0].Text;
    }
}

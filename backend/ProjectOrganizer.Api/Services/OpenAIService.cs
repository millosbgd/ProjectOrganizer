using OpenAI;
using OpenAI.Chat;

namespace ProjectOrganizer.Api.Services;

public class OpenAIService
{
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
}

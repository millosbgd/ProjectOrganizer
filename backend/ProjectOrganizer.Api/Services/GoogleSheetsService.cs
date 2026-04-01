using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Services;

public class GoogleSheetsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSheetsService> _logger;
    private SheetsService? _sheetsService;
    private DriveService? _driveService;

    private record OAuthCredentials(
        [property: System.Text.Json.Serialization.JsonPropertyName("client_id")] string ClientId,
        [property: System.Text.Json.Serialization.JsonPropertyName("client_secret")] string ClientSecret,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string RefreshToken
    );

    public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_sheetsService != null) return;

        string json;

        var environment = _configuration["Environment"] ?? "Local";

        if (environment == "Production")
        {
            var keyVaultUrl = _configuration["KeyVault:Url"]
                ?? throw new InvalidOperationException("KeyVault:Url nije podešen.");

            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync("GoogleOAuthCredentials");
            json = secret.Value.Value;
        }
        else
        {
            var keyPath = _configuration["GoogleSheets:LocalOAuthPath"]
                ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "google", "oauth_creds.json");
            json = await File.ReadAllTextAsync(keyPath);
        }

        var oauthData = System.Text.Json.JsonSerializer.Deserialize<OAuthCredentials>(json)
            ?? throw new InvalidOperationException("Nije moguće parsirati OAuth credentials.");

        var flowInitializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = oauthData.ClientId,
                ClientSecret = oauthData.ClientSecret
            },
            Scopes = new[] { SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive }
        };

        var flow = new GoogleAuthorizationCodeFlow(flowInitializer);
        var tokenResponse = new TokenResponse { RefreshToken = oauthData.RefreshToken };
        var credential = new UserCredential(flow, "user", tokenResponse);

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ProjectOrganizer"
        });

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ProjectOrganizer"
        });
    }

    public async Task<(string SpreadsheetId, List<SheetRowMapping> RowMappings)> CreateOrUpdateSheetAsync(
        Projekat projekat,
        List<ProjectImplementationItem> items,
        string? existingSpreadsheetId = null)
    {
        await EnsureInitializedAsync();

        string spreadsheetId;

        if (!string.IsNullOrEmpty(existingSpreadsheetId))
        {
            spreadsheetId = existingSpreadsheetId;
            await ClearSheetAsync(spreadsheetId);
        }
        else
        {
            spreadsheetId = await CreateSpreadsheetAsync(projekat.Naziv);
            await SetPublicAccessAsync(spreadsheetId);
        }

        var rowMappings = await WriteDataAsync(spreadsheetId, projekat, items);

        return (spreadsheetId, rowMappings);
    }

    private async Task<string> CreateSpreadsheetAsync(string naziv)
    {
        var spreadsheet = new Spreadsheet
        {
            Properties = new SpreadsheetProperties
            {
                Title = $"Implementacija - {naziv}"
            },
            Sheets = new List<Sheet>
            {
                new Sheet
                {
                    Properties = new SheetProperties { Title = "Stavke" }
                }
            }
        };

        var created = await _sheetsService!.Spreadsheets.Create(spreadsheet).ExecuteAsync();
        return created.SpreadsheetId;
    }

    private async Task SetPublicAccessAsync(string spreadsheetId)
    {
        var permission = new Google.Apis.Drive.v3.Data.Permission
        {
            Type = "anyone",
            Role = "writer"
        };
        await _driveService!.Permissions.Create(permission, spreadsheetId).ExecuteAsync();
    }

    private async Task ClearSheetAsync(string spreadsheetId)
    {
        var clearRequest = _sheetsService!.Spreadsheets.Values.Clear(
            new ClearValuesRequest(), spreadsheetId, "Stavke");
        await clearRequest.ExecuteAsync();
    }

    // Struktura za praćenje redova radi sync-a
    public record SheetRowMapping(int CheckListItemId, int RowIndex);

    private async Task<List<SheetRowMapping>> WriteDataAsync(string spreadsheetId, Projekat projekat, List<ProjectImplementationItem> items)
    {
        var values = new List<IList<object>>();
        var rowMappings = new List<SheetRowMapping>();

        // Kolone: A=Stavka impl., B=Checklist stavka, C=Završeno, D=Datum završetka, E=Klijent potvrdio, F=Klijent datum, G=ID (skriveno)
        values.Add(new List<object> { "Stavka implementacije", "Checklist stavka", "Završeno", "Datum završetka", "Klijent potvrdio", "Klijent datum", "ID" });

        int currentRow = 1; // 0-based, red 0 je header

        foreach (var item in items)
        {
            var itemNaziv = item.ImplementationItem?.Naziv ?? $"Stavka {item.Id}";

            if (item.CheckLists == null || !item.CheckLists.Any())
            {
                // Stavka bez checklist-a - prikaži samo parent red
                values.Add(new List<object> { itemNaziv, "", item.Zavrseno ? "TRUE" : "FALSE",
                    item.ZavrsenoDatum?.ToString("dd.MM.yyyy") ?? "",
                    item.KlijentPotvrdio ? "TRUE" : "FALSE",
                    item.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? "", "" });
                currentRow++;
            }
            else
            {
                // Parent red (group header) - bez checkboxa
                values.Add(new List<object> { itemNaziv, "", "", "", "", "", "" });
                currentRow++;

                // Checklist redovi
                foreach (var cl in item.CheckLists.OrderBy(c => c.Id))
                {
                    var clOpis = cl.CheckListItem?.Opis ?? $"Stavka {cl.Id}";
                    values.Add(new List<object> {
                        "",
                        clOpis,
                        cl.Zavrsen ? "TRUE" : "FALSE",
                        cl.ZavrsenDatum?.ToString("dd.MM.yyyy") ?? "",
                        cl.KlijentPotvrdio ? "TRUE" : "FALSE",
                        cl.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? "",
                        cl.Id.ToString()
                    });
                    rowMappings.Add(new SheetRowMapping(cl.Id, currentRow));
                    currentRow++;
                }
            }
        }

        var body = new ValueRange { Values = values };
        var updateRequest = _sheetsService!.Spreadsheets.Values.Update(
            body, spreadsheetId, "Stavke!A1");
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync();

        await FormatAndValidateSheetAsync(spreadsheetId, values.Count - 1, rowMappings);

        return rowMappings;
    }

    private async Task FormatAndValidateSheetAsync(string spreadsheetId, int dataRowCount, List<SheetRowMapping> rowMappings)
    {
        var spreadsheet = await _sheetsService!.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
        var sheetId = spreadsheet.Sheets[0].Properties.SheetId ?? 0;

        var requests = new List<Request>();

        // --- Header formatiranje (blue bold) ---
        requests.Add(new Request
        {
            RepeatCell = new RepeatCellRequest
            {
                Range = new GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 7 },
                Cell = new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        TextFormat = new TextFormat { Bold = true, ForegroundColor = new Color { Red = 1, Green = 1, Blue = 1 } },
                        BackgroundColor = new Color { Red = 0.23f, Green = 0.47f, Blue = 0.85f },
                        HorizontalAlignment = "CENTER"
                    }
                },
                Fields = "userEnteredFormat(textFormat,backgroundColor,horizontalAlignment)"
            }
        });

        // --- Checkbox za kolonu C (Završeno) - sve data redove ---
        if (dataRowCount > 0)
        {
            requests.Add(new Request
            {
                SetDataValidation = new SetDataValidationRequest
                {
                    Range = new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 2, EndColumnIndex = 3 },
                    Rule = new DataValidationRule { Condition = new BooleanCondition { Type = "BOOLEAN" }, ShowCustomUi = true }
                }
            });

            // --- Checkbox za kolonu E (Klijent potvrdio) - samo checklist redovi ---
            foreach (var mapping in rowMappings)
            {
                requests.Add(new Request
                {
                    SetDataValidation = new SetDataValidationRequest
                    {
                        Range = new GridRange { SheetId = sheetId, StartRowIndex = mapping.RowIndex, EndRowIndex = mapping.RowIndex + 1, StartColumnIndex = 4, EndColumnIndex = 5 },
                        Rule = new DataValidationRule { Condition = new BooleanCondition { Type = "BOOLEAN" }, ShowCustomUi = true }
                    }
                });
            }
        }

        // --- Širine kolona ---
        var columnWidths = new[] { 250, 280, 100, 130, 130, 120, 1 }; // G (ID) skoro nevidljiv
        for (int i = 0; i < columnWidths.Length; i++)
        {
            requests.Add(new Request
            {
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    Range = new DimensionRange { SheetId = sheetId, Dimension = "COLUMNS", StartIndex = i, EndIndex = i + 1 },
                    Properties = new DimensionProperties { PixelSize = columnWidths[i] },
                    Fields = "pixelSize"
                }
            });
        }

        // --- Freeze header ---
        requests.Add(new Request
        {
            UpdateSheetProperties = new UpdateSheetPropertiesRequest
            {
                Properties = new SheetProperties { SheetId = sheetId, GridProperties = new GridProperties { FrozenRowCount = 1 } },
                Fields = "gridProperties.frozenRowCount"
            }
        });

        // --- Boja za parent redove (group header - svijetlo plava) ---
        // Pronađi redove koji su parent (kolona A nije prazna, ali nisu header)
        // Koristimo conditional formatting: ako A != "" i red nije 1
        requests.Add(new Request
        {
            AddConditionalFormatRule = new AddConditionalFormatRuleRequest
            {
                Rule = new ConditionalFormatRule
                {
                    Ranges = new List<GridRange> { new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 0, EndColumnIndex = 7 } },
                    BooleanRule = new BooleanRule
                    {
                        Condition = new BooleanCondition
                        {
                            Type = "CUSTOM_FORMULA",
                            Values = new List<ConditionValue> { new ConditionValue { UserEnteredValue = "=$A2<>\"\"" } }
                        },
                        Format = new CellFormat { BackgroundColor = new Color { Red = 0.85f, Green = 0.91f, Blue = 0.97f } }
                    }
                },
                Index = 0
            }
        });

        // --- Boja za završene checklist redove (zelena) ---
        requests.Add(new Request
        {
            AddConditionalFormatRule = new AddConditionalFormatRuleRequest
            {
                Rule = new ConditionalFormatRule
                {
                    Ranges = new List<GridRange> { new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 0, EndColumnIndex = 6 } },
                    BooleanRule = new BooleanRule
                    {
                        Condition = new BooleanCondition
                        {
                            Type = "CUSTOM_FORMULA",
                            Values = new List<ConditionValue> { new ConditionValue { UserEnteredValue = "=$E2=TRUE" } }
                        },
                        Format = new CellFormat { BackgroundColor = new Color { Red = 0.85f, Green = 0.97f, Blue = 0.86f } }
                    }
                },
                Index = 1
            }
        });

        await _sheetsService.Spreadsheets.BatchUpdate(
            new BatchUpdateSpreadsheetRequest { Requests = requests },
            spreadsheetId).ExecuteAsync();
    }

    public async Task<List<(int CheckListItemId, bool KlijentPotvrdio)>> ReadSheetDataAsync(string spreadsheetId)
    {
        await EnsureInitializedAsync();

        var result = new List<(int, bool)>();

        // Čitamo kolone E (Klijent potvrdio) i G (ID)
        var response = await _sheetsService!.Spreadsheets.Values
            .Get(spreadsheetId, "Stavke!E2:G")
            .ExecuteAsync();

        if (response.Values == null) return result;

        foreach (var row in response.Values)
        {
            // Kolona G je index 2 u ovom range-u (E=0, F=1, G=2)
            if (row.Count < 3) continue;
            var idRaw = row[2]?.ToString();
            if (string.IsNullOrEmpty(idRaw) || !int.TryParse(idRaw, out var checkListItemId)) continue;

            var potvrdenoRaw = row[0]?.ToString();
            var potvrdeno = potvrdenoRaw == "TRUE";
            result.Add((checkListItemId, potvrdeno));
        }

        return result;
    }
}

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google.Apis.Auth.OAuth2;
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
            var secret = await client.GetSecretAsync("GoogleSheetsServiceAccount");
            json = secret.Value.Value;
        }
        else
        {
            // Lokalno: čitaj direktno iz fajla
            var keyPath = _configuration["GoogleSheets:LocalKeyPath"]
                ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "google", "key.json");
            json = await File.ReadAllTextAsync(keyPath);
        }

        var credential = GoogleCredential
            .FromJson(json)
            .CreateScoped(SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive);

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

    public async Task<string> CreateOrUpdateSheetAsync(
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
        }

        await WriteDataAsync(spreadsheetId, projekat, items);

        return spreadsheetId;
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

    private async Task ClearSheetAsync(string spreadsheetId)
    {
        var clearRequest = _sheetsService!.Spreadsheets.Values.Clear(
            new ClearValuesRequest(), spreadsheetId, "Stavke");
        await clearRequest.ExecuteAsync();
    }

    private async Task WriteDataAsync(string spreadsheetId, Projekat projekat, List<ProjectImplementationItem> items)
    {
        var values = new List<IList<object>>();

        // Header red
        values.Add(new List<object> { "Stavka implementacije", "Datum", "Potvrđeno" });

        // Stavke
        foreach (var item in items)
        {
            var naziv = item.ImplementationItem?.Naziv ?? $"Stavka {item.Id}";
            var datum = item.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? "";
            var potvrdeno = item.KlijentPotvrdio ? "TRUE" : "FALSE";
            values.Add(new List<object> { naziv, datum, potvrdeno });
        }

        var body = new ValueRange { Values = values };
        var updateRequest = _sheetsService!.Spreadsheets.Values.Update(
            body, spreadsheetId, "Stavke!A1");
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync();

        // Postavi checkboxove u kolonu C (od C2 nadalje)
        if (items.Count > 0)
        {
            await SetCheckboxesAsync(spreadsheetId, items.Count);
        }

        // Formatiranje header-a (bold, frozen)
        await FormatHeaderAsync(spreadsheetId);
    }

    private async Task SetCheckboxesAsync(string spreadsheetId, int itemCount)
    {
        var spreadsheet = await _sheetsService!.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
        var sheetId = spreadsheet.Sheets[0].Properties.SheetId ?? 0;

        var requests = new List<Request>
        {
            // Checkbox data validation za kolonu C
            new Request
            {
                SetDataValidation = new SetDataValidationRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 1,
                        EndRowIndex = 1 + itemCount,
                        StartColumnIndex = 2,
                        EndColumnIndex = 3
                    },
                    Rule = new DataValidationRule
                    {
                        Condition = new BooleanCondition { Type = "BOOLEAN" },
                        ShowCustomUi = true
                    }
                }
            }
        };

        await _sheetsService.Spreadsheets.BatchUpdate(
            new BatchUpdateSpreadsheetRequest { Requests = requests },
            spreadsheetId).ExecuteAsync();
    }

    private async Task FormatHeaderAsync(string spreadsheetId)
    {
        var spreadsheet = await _sheetsService!.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
        var sheetId = spreadsheet.Sheets[0].Properties.SheetId ?? 0;

        var requests = new List<Request>
        {
            // Bold header
            new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = 0,
                        EndRowIndex = 1,
                        StartColumnIndex = 0,
                        EndColumnIndex = 3
                    },
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            TextFormat = new TextFormat { Bold = true },
                            BackgroundColor = new Color { Red = 0.23f, Green = 0.47f, Blue = 0.85f },
                            HorizontalAlignment = "CENTER"
                        }
                    },
                    Fields = "userEnteredFormat(textFormat,backgroundColor,horizontalAlignment)"
                }
            },
            // Freeze header row
            new Request
            {
                UpdateSheetProperties = new UpdateSheetPropertiesRequest
                {
                    Properties = new SheetProperties
                    {
                        SheetId = sheetId,
                        GridProperties = new GridProperties { FrozenRowCount = 1 }
                    },
                    Fields = "gridProperties.frozenRowCount"
                }
            }
        };

        await _sheetsService.Spreadsheets.BatchUpdate(
            new BatchUpdateSpreadsheetRequest { Requests = requests },
            spreadsheetId).ExecuteAsync();
    }

    public async Task<List<(int ItemIndex, string? Datum, bool Potvrdeno)>> ReadSheetDataAsync(string spreadsheetId)
    {
        await EnsureInitializedAsync();

        var result = new List<(int, string?, bool)>();

        var response = await _sheetsService!.Spreadsheets.Values
            .Get(spreadsheetId, "Stavke!B2:C")
            .ExecuteAsync();

        if (response.Values == null) return result;

        for (int i = 0; i < response.Values.Count; i++)
        {
            var row = response.Values[i];
            var datum = row.Count > 0 ? row[0]?.ToString() : null;
            var potvrdenoRaw = row.Count > 1 ? row[1]?.ToString() : "FALSE";
            var potvrdeno = potvrdenoRaw == "TRUE";
            result.Add((i, datum, potvrdeno));
        }

        return result;
    }
}

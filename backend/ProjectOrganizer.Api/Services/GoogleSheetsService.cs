using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Google;
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

        // Kolone: A=Stavka impl., B=Checklist stavka, C=Klijent potvrdio, D=Klijent datum, E=ID (skriveno)
        values.Add(new List<object> { "Stavka implementacije", "Checklist stavka", "Klijent potvrdio", "Klijent datum", "ID" });

        int currentRow = 1; // 0-based, red 0 je header

        foreach (var item in items)
        {
            var itemNaziv = item.ImplementationItem?.Naziv ?? $"Stavka {item.Id}";

            if (item.CheckLists == null || !item.CheckLists.Any())
            {
                // Stavka bez checklist-a - prikaži samo parent red
                values.Add(new List<object> { itemNaziv, "",
                    item.KlijentPotvrdio ? "TRUE" : "FALSE",
                    item.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? "", "" });
                currentRow++;
            }
            else
            {
                // Parent red (group header) - bez checkboxa
                values.Add(new List<object> { itemNaziv, "", "", "", "" });
                currentRow++;

                // Checklist redovi
                foreach (var cl in item.CheckLists.OrderBy(c => c.Id))
                {
                    var clOpis = cl.CheckListItem?.Opis ?? $"Stavka {cl.Id}";
                    values.Add(new List<object> {
                        "",
                        clOpis,
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
        var getRequest = _sheetsService!.Spreadsheets.Get(spreadsheetId);
        getRequest.Fields = "sheets(properties(sheetId),protectedRanges(protectedRangeId),conditionalFormats)";
        var spreadsheet = await getRequest.ExecuteAsync();
        var sheetId = spreadsheet.Sheets[0].Properties.SheetId ?? 0;

        var requests = new List<Request>();

        // --- Obriši sve postojeće protected range-ove (ostaju od prethodnog generisanja) ---
        var existingProtections = spreadsheet.Sheets[0].ProtectedRanges;
        if (existingProtections != null)
        {
            foreach (var pr in existingProtections)
            {
                if (pr.ProtectedRangeId.HasValue)
                {
                    requests.Add(new Request
                    {
                        DeleteProtectedRange = new DeleteProtectedRangeRequest
                        {
                            ProtectedRangeId = pr.ProtectedRangeId.Value
                        }
                    });
                }
            }
        }

        // --- Obriši sve postojeće conditional format rules ---
        var existingRules = spreadsheet.Sheets[0].ConditionalFormats;
        if (existingRules != null)
        {
            // Brišemo od poslednjeg ka prvom da index-i ostanu validni
            for (int i = existingRules.Count - 1; i >= 0; i--)
            {
                requests.Add(new Request
                {
                    DeleteConditionalFormatRule = new DeleteConditionalFormatRuleRequest
                    {
                        SheetId = sheetId,
                        Index = i
                    }
                });
            }
        }

        // --- Obriši sve postojeće data validacije (ostaju od starih verzija) ---
        requests.Add(new Request
        {
            SetDataValidation = new SetDataValidationRequest
            {
                Range = new GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1000, StartColumnIndex = 0, EndColumnIndex = 5 }
                // Rule = null → briše sve validacije u opsegu
            }
        });

        // --- Header formatiranje (blue bold) ---
        requests.Add(new Request
        {
            RepeatCell = new RepeatCellRequest
            {
                Range = new GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 5 },
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

        // --- Checkbox za kolonu C (Klijent potvrdio) + validacija datuma na D - samo checklist redovi ---
        if (dataRowCount > 0)
        {
            foreach (var mapping in rowMappings)
            {
                // Checkbox na C
                requests.Add(new Request
                {
                    SetDataValidation = new SetDataValidationRequest
                    {
                        Range = new GridRange { SheetId = sheetId, StartRowIndex = mapping.RowIndex, EndRowIndex = mapping.RowIndex + 1, StartColumnIndex = 2, EndColumnIndex = 3 },
                        Rule = new DataValidationRule { Condition = new BooleanCondition { Type = "BOOLEAN" }, ShowCustomUi = true }
                    }
                });

                // Validacija datuma na D
                requests.Add(new Request
                {
                    SetDataValidation = new SetDataValidationRequest
                    {
                        Range = new GridRange { SheetId = sheetId, StartRowIndex = mapping.RowIndex, EndRowIndex = mapping.RowIndex + 1, StartColumnIndex = 3, EndColumnIndex = 4 },
                        Rule = new DataValidationRule
                        {
                            Condition = new BooleanCondition { Type = "DATE_IS_VALID" },
                            ShowCustomUi = true,
                            Strict = true
                        }
                    }
                });
            }

            // --- Format datuma dd.mm.yyyy za celu D kolonu (data redovi) ---
            requests.Add(new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 3, EndColumnIndex = 4 },
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            NumberFormat = new NumberFormat { Type = "DATE", Pattern = "dd.mm.yyyy" }
                        }
                    },
                    Fields = "userEnteredFormat.numberFormat"
                }
            });
        }

        // --- Zaštita: ceo sheet tab, jedino C+D za checklist redove su editabilni ---
        if (dataRowCount > 0)
        {
            var unprotectedRanges = rowMappings
                .Select(m => new GridRange
                {
                    SheetId = sheetId,
                    StartRowIndex = m.RowIndex,
                    EndRowIndex = m.RowIndex + 1,
                    StartColumnIndex = 2, // C (Klijent potvrdio)
                    EndColumnIndex = 4    // D (Klijent datum)
                })
                .ToList();

            // Range = samo SheetId bez row/col indexa = štiti ceo sheet tab
            // Na nivou celog sheet taba UnprotectedRanges funkcioniše ispravno
            // i sprečava brisanje redova i kolona
            requests.Add(new Request
            {
                AddProtectedRange = new AddProtectedRangeRequest
                {
                    ProtectedRange = new ProtectedRange
                    {
                        Range = new GridRange { SheetId = sheetId },
                        Description = "Edituj samo Klijent potvrdio (C) i datum (D)",
                        WarningOnly = false,
                        UnprotectedRanges = unprotectedRanges
                    }
                }
            });
        }

        // --- Širine kolona ---
        var columnWidths = new[] { 250, 280, 130, 120, 1 }; // E (ID) skoro nevidljiv
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
                    Ranges = new List<GridRange> { new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 0, EndColumnIndex = 5 } },
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
                    Ranges = new List<GridRange> { new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 0, EndColumnIndex = 5 } },
                    BooleanRule = new BooleanRule
                    {
                        Condition = new BooleanCondition
                        {
                            Type = "CUSTOM_FORMULA",
                            Values = new List<ConditionValue> { new ConditionValue { UserEnteredValue = "=$C2=TRUE" } }
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

    public async Task<List<(int CheckListItemId, bool KlijentPotvrdio, DateOnly? KlijentPotvrdioDatum)>> ReadSheetDataAsync(string spreadsheetId)
    {
        await EnsureInitializedAsync();

        var result = new List<(int, bool, DateOnly?)>();

        // Čitamo kolone C (Klijent potvrdio), D (Klijent datum), E (ID)
        var response = await _sheetsService!.Spreadsheets.Values
            .Get(spreadsheetId, "Stavke!C2:E")
            .ExecuteAsync();

        if (response.Values == null) return result;

        foreach (var row in response.Values)
        {
            // Kolona E je index 2 u ovom range-u (C=0, D=1, E=2)
            if (row.Count < 3) continue;
            var idRaw = row[2]?.ToString();
            if (string.IsNullOrEmpty(idRaw) || !int.TryParse(idRaw, out var checkListItemId)) continue;

            var potvrdenoRaw = row[0]?.ToString();
            var potvrdeno = potvrdenoRaw == "TRUE";

            DateOnly? datum = null;
            var datumRaw = row.Count > 1 ? row[1]?.ToString() : null;
            if (!string.IsNullOrEmpty(datumRaw) &&
                DateOnly.TryParseExact(datumRaw, new[] { "dd.MM.yyyy", "M/d/yyyy", "yyyy-MM-dd", "d.M.yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                datum = parsedDate;
            }

            result.Add((checkListItemId, potvrdeno, datum));
        }

        return result;
    }

    // ─── INTERNAL SHEET ──────────────────────────────────────────────────────

    public async Task<string> CreateOrUpdateInternalSheetAsync(
        Projekat projekat,
        List<ProjectImplementationItem> items,
        string? existingSpreadsheetId = null)
    {
        await EnsureInitializedAsync();

        string spreadsheetId;

        if (!string.IsNullOrEmpty(existingSpreadsheetId))
        {
            try
            {
                spreadsheetId = existingSpreadsheetId;
                await EnsureInternalSheetExistsAndClearAsync(spreadsheetId);
            }
            catch (GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(gex,
                    "Stored internal spreadsheet {SpreadsheetId} was not found. Creating a new internal sheet for project {ProjectId}.",
                    existingSpreadsheetId, projekat.Id);

                spreadsheetId = await CreateInternalSpreadsheetAsync(projekat);
            }
        }
        else
        {
            spreadsheetId = await CreateInternalSpreadsheetAsync(projekat);
        }

        await WriteInternalDataAsync(spreadsheetId, projekat, items);

        return spreadsheetId;
    }

    private async Task<string> CreateInternalSpreadsheetAsync(Projekat projekat)
    {
        var spreadsheet = new Spreadsheet
        {
            Properties = new SpreadsheetProperties
            {
                Title = $"Interni izveštaj - {projekat.Naziv}"
            },
            Sheets = new List<Sheet>
            {
                new Sheet
                {
                    Properties = new SheetProperties { Title = "Interni" }
                }
            }
        };

        var created = await _sheetsService!.Spreadsheets.Create(spreadsheet).ExecuteAsync();

        var permission = new Google.Apis.Drive.v3.Data.Permission
        {
            Type = "anyone",
            Role = "reader"
        };
        await _driveService!.Permissions.Create(permission, created.SpreadsheetId).ExecuteAsync();

        return created.SpreadsheetId;
    }

    private async Task EnsureInternalSheetExistsAndClearAsync(string spreadsheetId)
    {
        var getRequest = _sheetsService!.Spreadsheets.Get(spreadsheetId);
        getRequest.Fields = "sheets(properties(title))";
        var spreadsheet = await getRequest.ExecuteAsync();

        var hasInternalSheet = spreadsheet.Sheets.Any(s => s.Properties.Title == "Interni");
        if (!hasInternalSheet)
        {
            await _sheetsService.Spreadsheets.BatchUpdate(
                new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>
                    {
                        new Request
                        {
                            AddSheet = new AddSheetRequest
                            {
                                Properties = new SheetProperties { Title = "Interni" }
                            }
                        }
                    }
                },
                spreadsheetId).ExecuteAsync();
        }

        var clearRequest = _sheetsService.Spreadsheets.Values.Clear(
            new ClearValuesRequest(), spreadsheetId, "Interni");
        await clearRequest.ExecuteAsync();
    }

    private async Task WriteInternalDataAsync(string spreadsheetId, Projekat projekat, List<ProjectImplementationItem> items)
    {
        var values = new List<IList<object>>();

        // Kolone: A=Stavka impl., B=Stavka čekliste, C=Planirani rok, D=Planirano %, E=Realizovano %, F=Datum završetka, G=Klijent potvrdio, H=Klijent datum
        values.Add(new List<object>
        {
            "Stavka implementacije", "Stavka čekliste",
            "Planirani rok", "Planirano %", "Realizovano %",
            "Datum završetka", "Klijent potvrdio", "Klijent datum"
        });

        int dataRowCount = 0;
        var parentRowIndexes = new List<int>(); // 0-based row indexes of group headers

        foreach (var item in items)
        {
            var itemNaziv = item.ImplementationItem?.Naziv ?? $"Stavka {item.Id}";

            if (item.CheckLists == null || !item.CheckLists.Any())
            {
                // Stavka bez checklist-a
                values.Add(new List<object>
                {
                    itemNaziv,
                    "",
                    "",
                    "",
                    item.Zavrseno ? "100%" : "0%",
                    item.ZavrsenoDatum?.ToString("dd.MM.yyyy") ?? "",
                    item.KlijentPotvrdio ? "DA" : "NE",
                    item.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? ""
                });
                parentRowIndexes.Add(values.Count - 1); // treat as parent for coloring
                dataRowCount++;
            }
            else
            {
                var sortedChecklists = item.CheckLists.OrderBy(c => c.Id).ToList();

                decimal totalPlanirano = sortedChecklists.Sum(cl => cl.Procenat ?? 0);
                decimal totalRealizovano = sortedChecklists.Where(cl => cl.Zavrsen).Sum(cl => cl.Procenat ?? 0);

                // Parent (group header) red
                values.Add(new List<object>
                {
                    itemNaziv,
                    "",
                    "",
                    FormatProcenat(totalPlanirano),
                    FormatProcenat(totalRealizovano),
                    item.ZavrsenoDatum?.ToString("dd.MM.yyyy") ?? "",
                    item.KlijentPotvrdio ? "DA" : "NE",
                    item.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? ""
                });
                parentRowIndexes.Add(values.Count - 1);
                dataRowCount++;

                // Checklist redovi
                foreach (var cl in sortedChecklists)
                {
                    var clOpis = cl.CheckListItemId == -1
                        ? (cl.Opis ?? $"Custom {cl.Id}")
                        : (cl.CheckListItem?.Opis ?? $"Stavka {cl.Id}");

                    values.Add(new List<object>
                    {
                        "",
                        clOpis,
                        cl.PlaniraniRok?.ToString("dd.MM.yyyy") ?? "",
                        FormatProcenat(cl.Procenat ?? 0),
                        cl.Zavrsen ? FormatProcenat(cl.Procenat ?? 0) : "0%",
                        cl.ZavrsenDatum?.ToString("dd.MM.yyyy") ?? "",
                        cl.KlijentPotvrdio ? "DA" : "NE",
                        cl.KlijentPotvrdioDatum?.ToString("dd.MM.yyyy") ?? ""
                    });
                    dataRowCount++;
                }
            }
        }

        var body = new ValueRange { Values = values };
        var updateRequest = _sheetsService!.Spreadsheets.Values.Update(
            body, spreadsheetId, "Interni!A1");
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await updateRequest.ExecuteAsync();

        await FormatInternalSheetAsync(spreadsheetId, dataRowCount, parentRowIndexes);
    }

    private static string FormatProcenat(decimal value) => $"{value:0.##}%";

    private async Task FormatInternalSheetAsync(string spreadsheetId, int dataRowCount, List<int> parentRowIndexes)
    {
        var getRequest = _sheetsService!.Spreadsheets.Get(spreadsheetId);
        getRequest.Fields = "sheets(properties(sheetId,title),protectedRanges(protectedRangeId),conditionalFormats)";
        var spreadsheet = await getRequest.ExecuteAsync();
        var sheetEntry = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == "Interni")
                         ?? spreadsheet.Sheets[0];
        var sheetId = sheetEntry.Properties.SheetId ?? 0;

        var requests = new List<Request>();

        // Obriši postojeće protections i conditional formats
        var existingProtections = sheetEntry?.ProtectedRanges;
        if (existingProtections != null)
            foreach (var pr in existingProtections)
                if (pr.ProtectedRangeId.HasValue)
                    requests.Add(new Request { DeleteProtectedRange = new DeleteProtectedRangeRequest { ProtectedRangeId = pr.ProtectedRangeId.Value } });

        var existingRules = sheetEntry?.ConditionalFormats;
        if (existingRules != null)
            for (int i = existingRules.Count - 1; i >= 0; i--)
                requests.Add(new Request { DeleteConditionalFormatRule = new DeleteConditionalFormatRuleRequest { SheetId = sheetId, Index = i } });

        // Header (tamno zelena, bold, beli tekst)
        requests.Add(new Request
        {
            RepeatCell = new RepeatCellRequest
            {
                Range = new GridRange { SheetId = sheetId, StartRowIndex = 0, EndRowIndex = 1, StartColumnIndex = 0, EndColumnIndex = 8 },
                Cell = new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        TextFormat = new TextFormat { Bold = true, ForegroundColor = new Color { Red = 1, Green = 1, Blue = 1 } },
                        BackgroundColor = new Color { Red = 0.13f, Green = 0.53f, Blue = 0.33f },
                        HorizontalAlignment = "CENTER"
                    }
                },
                Fields = "userEnteredFormat(textFormat,backgroundColor,horizontalAlignment)"
            }
        });

        // Parent redovi (group header) — svetlo plava pozadina, bold
        foreach (var rowIdx in parentRowIndexes)
        {
            requests.Add(new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange { SheetId = sheetId, StartRowIndex = rowIdx, EndRowIndex = rowIdx + 1, StartColumnIndex = 0, EndColumnIndex = 8 },
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            TextFormat = new TextFormat { Bold = true },
                            BackgroundColor = new Color { Red = 0.85f, Green = 0.91f, Blue = 0.97f }
                        }
                    },
                    Fields = "userEnteredFormat(textFormat,backgroundColor)"
                }
            });
        }

        if (dataRowCount > 0)
        {
            // Conditional format: završeni checklist redovi → svetlo zelena
            requests.Add(new Request
            {
                AddConditionalFormatRule = new AddConditionalFormatRuleRequest
                {
                    Rule = new ConditionalFormatRule
                    {
                        Ranges = new List<GridRange> { new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 0, EndColumnIndex = 8 } },
                        BooleanRule = new BooleanRule
                        {
                            Condition = new BooleanCondition
                            {
                                Type = "CUSTOM_FORMULA",
                                Values = new List<ConditionValue> { new ConditionValue { UserEnteredValue = "=$G2=\"DA\"" } }
                            },
                            Format = new CellFormat { BackgroundColor = new Color { Red = 0.84f, Green = 0.94f, Blue = 1.0f } }
                        }
                    },
                    Index = 0
                }
            });

            requests.Add(new Request
            {
                AddConditionalFormatRule = new AddConditionalFormatRuleRequest
                {
                    Rule = new ConditionalFormatRule
                    {
                        Ranges = new List<GridRange> { new GridRange { SheetId = sheetId, StartRowIndex = 1, EndRowIndex = 1 + dataRowCount, StartColumnIndex = 0, EndColumnIndex = 8 } },
                        BooleanRule = new BooleanRule
                        {
                            Condition = new BooleanCondition
                            {
                                Type = "CUSTOM_FORMULA",
                                Values = new List<ConditionValue> { new ConditionValue { UserEnteredValue = "=$F2<>\"\"" } }
                            },
                            Format = new CellFormat { BackgroundColor = new Color { Red = 0.85f, Green = 0.97f, Blue = 0.86f } }
                        }
                    },
                    Index = 1
                }
            });
        }

        // Zaštita - ceo sheet je read-only
        requests.Add(new Request
        {
            AddProtectedRange = new AddProtectedRangeRequest
            {
                ProtectedRange = new ProtectedRange
                {
                    Range = new GridRange { SheetId = sheetId },
                    Description = "Interni izveštaj - read only",
                    WarningOnly = false
                }
            }
        });

        // Širine kolona: A=240, B=260, C=110, D=90, E=100, F=120, G=110, H=120
        var columnWidths = new[] { 240, 260, 110, 90, 100, 120, 110, 120 };
        for (int i = 0; i < columnWidths.Length; i++)
            requests.Add(new Request
            {
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    Range = new DimensionRange { SheetId = sheetId, Dimension = "COLUMNS", StartIndex = i, EndIndex = i + 1 },
                    Properties = new DimensionProperties { PixelSize = columnWidths[i] },
                    Fields = "pixelSize"
                }
            });

        // Freeze header
        requests.Add(new Request
        {
            UpdateSheetProperties = new UpdateSheetPropertiesRequest
            {
                Properties = new SheetProperties { SheetId = sheetId, GridProperties = new GridProperties { FrozenRowCount = 1 } },
                Fields = "gridProperties.frozenRowCount"
            }
        });

        await _sheetsService.Spreadsheets.BatchUpdate(
            new BatchUpdateSpreadsheetRequest { Requests = requests },
            spreadsheetId).ExecuteAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ProjectOrganizer.Api.Services;

public class DevOpsSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EncryptionService _encryptionService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<DevOpsSyncService> _logger;

    public DevOpsSyncService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        EncryptionService encryptionService,
        NotificationService notificationService,
        ILogger<DevOpsSyncService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<DevOpsDailySyncResult> RefreshAllLinkedTasksAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _context.DevOpsTasksCandidates
            .Include(t => t.User)
            .Where(t => t.DevOpsUrl != null && t.DevOpsUrl != "")
            .OrderBy(t => t.UserId)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var result = new DevOpsDailySyncResult { TotalTasks = candidates.Count };

        foreach (var userGroup in candidates.GroupBy(t => t.UserId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = userGroup.First().User ?? await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userGroup.Key, cancellationToken);

            if (user == null)
            {
                result.SkippedTasks += userGroup.Count();
                continue;
            }

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == user.Auth0Id, cancellationToken);

            if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.DevOpsPersonalAccessToken))
            {
                result.SkippedTasks += userGroup.Count();
                _logger.LogInformation("DevOps sync preskočen za korisnika {UserId}: PAT nije podešen.", user.Id);
                continue;
            }

            var userResult = await RefreshUserTasksAsync(
                user.Id,
                userGroup.ToList(),
                userSettings.DevOpsPersonalAccessToken,
                cancellationToken);

            result.RefreshedTasks += userResult.RefreshedTasks;
            result.FailedTasks += userResult.FailedTasks;
            result.SkippedTasks += userResult.SkippedTasks;

            if (userResult.RefreshedTasks > 0 || userResult.FailedTasks > 0)
            {
                await SendDailySummaryNotificationAsync(user.Id, userResult, cancellationToken);
            }
        }

        return result;
    }

    public Task<bool> HasLinkedTaskSyncSinceAsync(DateTime utcFrom, CancellationToken cancellationToken = default)
    {
        return _context.DevOpsTasksCandidates
            .AnyAsync(t =>
                t.DevOpsUrl != null &&
                t.DevOpsUrl != "" &&
                t.LastDevOpsSyncAt >= utcFrom,
                cancellationToken);
    }

    private async Task<DevOpsDailySyncResult> RefreshUserTasksAsync(
        int userId,
        IReadOnlyCollection<DevOpsTasksCandidate> candidates,
        string encryptedPat,
        CancellationToken cancellationToken)
    {
        var result = new DevOpsDailySyncResult { TotalTasks = candidates.Count };
        var client = CreateDevOpsClient(encryptedPat);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (string.IsNullOrWhiteSpace(candidate.DevOpsUrl) ||
                    !TryParseDevOpsUrl(candidate.DevOpsUrl, out var organization, out var project, out var workItemId))
                {
                    candidate.LastDevOpsSyncAt = DateTime.UtcNow;
                    candidate.LastDevOpsSyncStatus = "Skipped";
                    candidate.LastDevOpsSyncError = "Nevažeći Azure DevOps URL.";
                    result.SkippedTasks++;
                    continue;
                }

                var apiUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}?$expand=all&api-version=7.1";
                var response = await client.GetAsync(apiUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    candidate.LastDevOpsSyncAt = DateTime.UtcNow;
                    candidate.LastDevOpsSyncStatus = "Failed";
                    candidate.LastDevOpsSyncError = $"Azure DevOps API: {(int)response.StatusCode} {response.ReasonPhrase}";
                    result.FailedTasks++;
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var fields = doc.RootElement.GetProperty("fields");

                candidate.DevOpsWorkItemId = workItemId;
                candidate.Title = GetFieldString(fields, "System.Title") ?? candidate.Title;
                candidate.Description = StripHtml(GetFieldString(fields, "System.Description")) ?? candidate.Description;
                candidate.AcceptanceCriteria = StripHtml(GetFieldString(fields, "Microsoft.VSTS.Common.AcceptanceCriteria")) ?? candidate.AcceptanceCriteria;
                candidate.Priority = MapPriority(fields) ?? candidate.Priority;
                candidate.Estimation = MapEstimation(fields) ?? candidate.Estimation;
                candidate.DevOpsState = GetFieldString(fields, "System.State") ?? candidate.DevOpsState;
                candidate.DevOpsAssignedTo = GetIdentityDisplayName(fields, "System.AssignedTo") ?? candidate.DevOpsAssignedTo;
                candidate.DevOpsChangedDate = GetFieldDateTime(fields, "System.ChangedDate") ?? candidate.DevOpsChangedDate;
                candidate.LastDevOpsSyncAt = DateTime.UtcNow;
                candidate.LastDevOpsSyncStatus = "Success";
                candidate.LastDevOpsSyncError = null;

                await FetchAndSaveStatusHistoryAsync(organization, project, workItemId, candidate.Id, client, cancellationToken, candidate.DevOpsState);

                result.RefreshedTasks++;
            }
            catch (Exception ex)
            {
                candidate.LastDevOpsSyncAt = DateTime.UtcNow;
                candidate.LastDevOpsSyncStatus = "Failed";
                candidate.LastDevOpsSyncError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                result.FailedTasks++;
                _logger.LogWarning(ex, "DevOps sync nije uspeo za lokalni task {CandidateId} korisnika {UserId}.", candidate.Id, userId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task SendDailySummaryNotificationAsync(
        int userId,
        DevOpsDailySyncResult result,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var message = $"Osveženo je {result.RefreshedTasks} vaših taskova sa DevOps-a.";

        if (result.FailedTasks > 0)
        {
            message += $" {result.FailedTasks} nije osveženo.";
        }

        await _notificationService.CreateAndSendAsync(
            userId: userId,
            type: "DevOpsSync",
            message: message,
            referenceKey: $"DevOpsDailySync:{userId}:{today}");
    }

    private HttpClient CreateDevOpsClient(string encryptedPat)
    {
        var decryptedPat = _encryptionService.Decrypt(encryptedPat);
        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{decryptedPat}"));
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task FetchAndSaveStatusHistoryAsync(
        string organization,
        string project,
        int workItemId,
        int candidateId,
        HttpClient client,
        CancellationToken cancellationToken,
        string? fallbackState = null)
    {
        var updatesUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}/updates?api-version=7.1";
        var updatesResponse = await client.GetAsync(updatesUrl, cancellationToken);
        if (!updatesResponse.IsSuccessStatusCode) return;

        var updatesJson = await updatesResponse.Content.ReadAsStringAsync(cancellationToken);
        using var updatesDoc = JsonDocument.Parse(updatesJson);

        var updates = updatesDoc.RootElement.GetProperty("value").EnumerateArray()
            .Select(update => new
            {
                Update = update,
                RevisedDate = update.TryGetProperty("revisedDate", out var revisedDateEl)
                    && DateTime.TryParse(revisedDateEl.GetString(), out var revisedDate)
                    ? revisedDate
                    : (DateTime?)null
            })
            .Where(update => update.RevisedDate.HasValue && update.RevisedDate.Value.Year < 9999)
            .OrderBy(update => update.RevisedDate)
            .ToList();
        var initialState = updates
            .Select(update => TryGetUpdateFieldValue(update.Update, "System.State", "oldValue"))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? fallbackState;
        var initialAssignedTo = updates
            .Select(update => TryGetUpdateFieldValue(update.Update, "System.AssignedTo", "oldValue"))
            .FirstOrDefault(value => value != null);

        var timeline = new List<(DateTime StartedAt, string State, string? AssignedTo)>();
        string? currentState = initialState;
        string? currentAssignedTo = initialAssignedTo;

        foreach (var updateInfo in updates)
        {
            var update = updateInfo.Update;
            var revisedDate = updateInfo.RevisedDate.Value;
            if (!update.TryGetProperty("fields", out var updFields)) continue;

            if (timeline.Count == 0 && currentState != null)
            {
                timeline.Add((revisedDate, currentState, currentAssignedTo));
            }

            var changed = false;

            if (updFields.TryGetProperty("System.State", out var stateEl))
            {
                var newState = GetUpdateFieldValue(stateEl, "newValue");

                if (newState != null && newState != currentState)
                {
                    currentState = newState;
                    changed = true;
                }
            }

            if (updFields.TryGetProperty("System.AssignedTo", out var assignedEl))
            {
                var newAssigned = GetUpdateFieldValue(assignedEl, "newValue");

                if (newAssigned != currentAssignedTo)
                {
                    currentAssignedTo = newAssigned;
                    changed = true;
                }
            }

            if (changed && currentState != null)
            {
                timeline.Add((revisedDate, currentState, currentAssignedTo));
            }
        }

        var rawEntries = new List<(string Status, string? AssignedTo, DateTime StartedAt, DateTime? EndedAt, int? DurationMinutes)>();
        for (var i = 0; i < timeline.Count; i++)
        {
            var (startedAt, state, assignedTo) = timeline[i];
            DateTime? endedAt = null;
            int? durationMinutes = null;
            if (i + 1 < timeline.Count)
            {
                endedAt = timeline[i + 1].StartedAt;
                if (endedAt > startedAt)
                {
                    durationMinutes = (int)(endedAt.Value - startedAt).TotalMinutes;
                }
            }

            rawEntries.Add((state, assignedTo, startedAt, endedAt, durationMinutes));
        }

        var existing = await _context.DevOpsTaskStatusHistory
            .Where(h => h.DevOpsTaskCandidateId == candidateId)
            .ToListAsync(cancellationToken);

        _context.DevOpsTaskStatusHistory.RemoveRange(existing);
        _context.DevOpsTaskStatusHistory.AddRange(rawEntries.Select(e => new DevOpsTaskStatusHistory
        {
            DevOpsTaskCandidateId = candidateId,
            Status = e.Status,
            AssignedTo = e.AssignedTo,
            ChangedDate = e.StartedAt,
            StartedAt = e.StartedAt,
            EndedAt = e.EndedAt,
            DurationMinutes = e.DurationMinutes
        }));
    }

    private static string? TryGetUpdateFieldValue(JsonElement update, string fieldName, string valueName)
    {
        if (!update.TryGetProperty("fields", out var fields)) return null;
        if (!fields.TryGetProperty(fieldName, out var field)) return null;
        return GetUpdateFieldValue(field, valueName);
    }

    private static string? GetUpdateFieldValue(JsonElement field, string valueName)
    {
        if (!field.TryGetProperty(valueName, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind == JsonValueKind.Object && value.TryGetProperty("displayName", out var displayName)
            ? displayName.GetString()
            : value.GetString();
    }

    private static bool TryParseDevOpsUrl(string url, out string organization, out string project, out int workItemId)
    {
        organization = string.Empty;
        project = string.Empty;
        workItemId = 0;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath.TrimStart('/').TrimEnd('/').Split('/');

        if (uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length < 5) return false;
            organization = segments[0];
            project = segments[1];
            return int.TryParse(segments[^1], out workItemId);
        }

        if (uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase))
        {
            organization = uri.Host.Replace(".visualstudio.com", string.Empty);
            if (segments.Length < 4) return false;
            project = segments[0];
            return int.TryParse(segments[^1], out workItemId);
        }

        return false;
    }

    private static string? GetFieldString(JsonElement fields, string fieldName)
    {
        if (fields.TryGetProperty(fieldName, out var element) && element.ValueKind != JsonValueKind.Null)
            return element.GetString();
        return null;
    }

    private static DateTime? GetFieldDateTime(JsonElement fields, string fieldName)
    {
        var value = GetFieldString(fields, fieldName);
        return DateTime.TryParse(value, out var dateTime) ? dateTime : null;
    }

    private static string? GetIdentityDisplayName(JsonElement fields, string fieldName)
    {
        if (!fields.TryGetProperty(fieldName, out var element) || element.ValueKind == JsonValueKind.Null)
            return null;

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("displayName", out var displayName) &&
            displayName.ValueKind == JsonValueKind.String)
        {
            return displayName.GetString();
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    private static string? MapPriority(JsonElement fields)
    {
        if (fields.TryGetProperty("Microsoft.VSTS.Common.Priority", out var el) && el.ValueKind != JsonValueKind.Null)
        {
            return el.GetInt32() switch
            {
                1 => "Critical",
                2 => "High",
                3 => "Medium",
                4 => "Low",
                _ => null
            };
        }
        return null;
    }

    private static string? MapEstimation(JsonElement fields)
    {
        if (fields.TryGetProperty("Microsoft.VSTS.Scheduling.StoryPoints", out var sp) && sp.ValueKind != JsonValueKind.Null)
            return $"{sp.GetDouble()} sp";

        if (fields.TryGetProperty("Microsoft.VSTS.Scheduling.OriginalEstimate", out var oe) && oe.ValueKind != JsonValueKind.Null)
            return $"{oe.GetDouble()} h";

        return null;
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;
        return System.Net.WebUtility.HtmlDecode(html);
    }
}

public class DevOpsDailySyncResult
{
    public int TotalTasks { get; set; }
    public int RefreshedTasks { get; set; }
    public int SkippedTasks { get; set; }
    public int FailedTasks { get; set; }
}

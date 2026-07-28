using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevOpsTasksCandidatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<DevOpsTasksCandidatesController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EncryptionService _encryptionService;
    private readonly DevOpsSyncService _devOpsSyncService;

    public DevOpsTasksCandidatesController(
        ApplicationDbContext context,
        UserService userService,
        ILogger<DevOpsTasksCandidatesController> logger,
        IHttpClientFactory httpClientFactory,
        EncryptionService encryptionService,
        DevOpsSyncService devOpsSyncService)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _devOpsSyncService = devOpsSyncService;
    }

    // GET: api/devopstaskscandidates/aktivnost/{aktivnostId}
    [HttpGet("aktivnost/{aktivnostId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetCandidatesForAktivnost(int aktivnostId)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var candidateEntities = await _context.DevOpsTasksCandidates
                .Include(c => c.User)
                .Include(c => c.StatusHistory)
                .Where(c => c.AktivnostId == aktivnostId)
                .OrderBy(c => c.OrderIndex)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            var roleDict = await _context.DevOpsUsers
                .Where(u => u.RoleId != null)
                .Join(_context.Codebooks, u => u.RoleId, cb => cb.Id, (u, cb) => new { u.DisplayName, cb.Value })
                .GroupBy(x => x.DisplayName)
                .Select(g => new { DisplayName = g.Key, RoleName = g.First().Value })
                .ToDictionaryAsync(x => x.DisplayName, x => x.RoleName);

            var candidates = candidateEntities.Select(c => new
            {
                c.Id,
                c.AktivnostId,
                c.UserId,
                c.Title,
                c.Description,
                c.AcceptanceCriteria,
                c.Priority,
                c.Estimation,
                c.OrderIndex,
                c.Status,
                c.CreatedAt,
                c.DevOpsWorkItemId,
                c.DevOpsUrl,
                c.DevOpsState,
                c.DevOpsAssignedTo,
                c.DevOpsChangedDate,
                c.LastDevOpsSyncAt,
                c.LastDevOpsSyncStatus,
                c.LastDevOpsSyncError,
                User = c.User == null ? null : new
                {
                    c.User.Id,
                    c.User.Email,
                    c.User.Name
                },
                StatusHistory = (c.StatusHistory ?? Enumerable.Empty<DevOpsTaskStatusHistory>())
                    .GroupBy(h => new { h.AssignedTo, h.Status })
                    .OrderByDescending(g => g.Max(h => h.StartedAt))
                    .Select(g => new
                    {
                        g.Key.Status,
                        g.Key.AssignedTo,
                        RoleName = g.Key.AssignedTo != null && roleDict.TryGetValue(g.Key.AssignedTo, out var r) ? r : null,
                        TotalDurationMinutes = g.Where(h => h.DurationMinutes.HasValue).Sum(h => h.DurationMinutes),
                        IsActive = g.Any(h => !h.DurationMinutes.HasValue),
                        StartedAt = g.Min(h => h.StartedAt),
                        EndedAt = g.Any(h => !h.EndedAt.HasValue) ? null : g.Max(h => h.EndedAt),
                        LastStartedAt = g.Max(h => h.StartedAt)
                    })
                    .ToList()
            });

            return Ok(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting candidates for aktivnost {aktivnostId}");
            return StatusCode(500, "Error retrieving candidates");
        }
    }

    // GET: api/devopstaskscandidates/project/{projectId}
    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetCandidatesForProject(int projectId)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            if (currentUser.Role != "Admin")
            {
                var hasPermission = await _context.ProjectPermissions
                    .AnyAsync(p => p.ProjekatId == projectId && p.UserId == currentUser.Id);

                if (!hasPermission)
                    return Forbid();
            }

            var candidateEntities = await _context.DevOpsTasksCandidates
                .Include(c => c.User)
                .Include(c => c.StatusHistory)
                .Include(c => c.Aktivnost)
                .Where(c => c.Aktivnost != null && c.Aktivnost.ProjekatId == projectId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenBy(c => c.OrderIndex)
                .ToListAsync();

            var roleDict = await _context.DevOpsUsers
                .Where(u => u.RoleId != null)
                .Join(_context.Codebooks, u => u.RoleId, cb => cb.Id, (u, cb) => new { u.DisplayName, cb.Value })
                .GroupBy(x => x.DisplayName)
                .Select(g => new { DisplayName = g.Key, RoleName = g.First().Value })
                .ToDictionaryAsync(x => x.DisplayName, x => x.RoleName);

            var candidates = candidateEntities.Select(c => new
            {
                c.Id,
                c.AktivnostId,
                c.UserId,
                c.Title,
                c.Description,
                c.AcceptanceCriteria,
                c.Priority,
                c.Estimation,
                c.OrderIndex,
                c.Status,
                c.CreatedAt,
                c.DevOpsWorkItemId,
                c.DevOpsUrl,
                c.DevOpsState,
                c.DevOpsAssignedTo,
                c.DevOpsChangedDate,
                c.LastDevOpsSyncAt,
                c.LastDevOpsSyncStatus,
                c.LastDevOpsSyncError,
                Aktivnost = c.Aktivnost == null ? null : new
                {
                    c.Aktivnost.Id,
                    c.Aktivnost.Opis,
                    c.Aktivnost.Datum,
                    c.Aktivnost.Status,
                    c.Aktivnost.Vrsta
                },
                User = c.User == null ? null : new
                {
                    c.User.Id,
                    c.User.Email,
                    c.User.Name
                },
                StatusHistory = (c.StatusHistory ?? Enumerable.Empty<DevOpsTaskStatusHistory>())
                    .GroupBy(h => new { h.AssignedTo, h.Status })
                    .OrderByDescending(g => g.Max(h => h.StartedAt))
                    .Select(g => new
                    {
                        g.Key.Status,
                        g.Key.AssignedTo,
                        RoleName = g.Key.AssignedTo != null && roleDict.TryGetValue(g.Key.AssignedTo, out var r) ? r : null,
                        TotalDurationMinutes = g.Where(h => h.DurationMinutes.HasValue).Sum(h => h.DurationMinutes),
                        IsActive = g.Any(h => !h.DurationMinutes.HasValue),
                        StartedAt = g.Min(h => h.StartedAt),
                        EndedAt = g.Any(h => !h.EndedAt.HasValue) ? null : g.Max(h => h.EndedAt),
                        LastStartedAt = g.Max(h => h.StartedAt)
                    })
                    .ToList()
            });

            return Ok(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DevOps task candidates for project {ProjectId}", projectId);
            return StatusCode(500, "Error retrieving project task candidates");
        }
    }

    // GET: api/devopstaskscandidates/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<DevOpsTasksCandidate>> GetCandidate(int id)
    {
        try
        {
            var candidate = await _context.DevOpsTasksCandidates
                .Include(c => c.User)
                .Include(c => c.Aktivnost)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidate == null)
                return NotFound();

            return Ok(candidate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting candidate {id}");
            return StatusCode(500, "Error retrieving candidate");
        }
    }

    // PUT: api/devopstaskscandidates/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCandidate(int id, [FromBody] UpdateDevOpsTaskCandidateDto dto)
    {
        try
        {
            var candidate = await _context.DevOpsTasksCandidates.FindAsync(id);
            if (candidate == null)
                return NotFound();

            candidate.Title = dto.Title;
            candidate.Description = dto.Description;
            candidate.AcceptanceCriteria = dto.AcceptanceCriteria;
            candidate.Priority = dto.Priority;
            candidate.Estimation = dto.Estimation;
            candidate.OrderIndex = dto.OrderIndex;
            candidate.Status = dto.Status;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Candidate {id} updated");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating candidate {id}");
            return StatusCode(500, "Error updating candidate");
        }
    }

    // PUT: api/devopstaskscandidates/{id}/status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var candidate = await _context.DevOpsTasksCandidates.FindAsync(id);
            if (candidate == null)
                return NotFound();

            candidate.Status = dto.Status;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Candidate {id} status updated to {dto.Status}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating candidate {id} status");
            return StatusCode(500, "Error updating status");
        }
    }

    // DELETE: api/devopstaskscandidates/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCandidate(int id)
    {
        try
        {
            var candidate = await _context.DevOpsTasksCandidates.FindAsync(id);
            if (candidate == null)
                return NotFound();

            _context.DevOpsTasksCandidates.Remove(candidate);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Candidate {id} deleted");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting candidate {id}");
            return StatusCode(500, "Error deleting candidate");
        }
    }

    // POST: api/devopstaskscandidates
    [HttpPost]
    public async Task<ActionResult<object>> CreateCandidate([FromBody] CreateDevOpsTaskCandidateDto dto)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var candidate = new DevOpsTasksCandidate
            {
                AktivnostId = dto.AktivnostId,
                UserId = currentUser.Id,
                Title = dto.Title,
                Description = dto.Description,
                AcceptanceCriteria = dto.AcceptanceCriteria,
                Priority = dto.Priority,
                Estimation = dto.Estimation,
                OrderIndex = dto.OrderIndex,
                Status = "Draft",
                DevOpsWorkItemId = dto.DevOpsWorkItemId,
                DevOpsUrl = dto.DevOpsUrl
            };

            _context.DevOpsTasksCandidates.Add(candidate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New candidate created for aktivnost {AktivnostId}", dto.AktivnostId);

            return Ok(new { candidate.Id, candidate.AktivnostId, candidate.Title, candidate.Status, candidate.CreatedAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating candidate");
            return StatusCode(500, "Error creating candidate");
        }
    }

    // POST: api/devopstaskscandidates/fetch-from-url
    [HttpPost("fetch-from-url")]
    public async Task<ActionResult<FetchedDevOpsTaskDto>> FetchFromDevOpsUrl([FromBody] FetchFromDevOpsUrlDto dto)
    {
        try
        {
            var auth0Id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(auth0Id))
                return Unauthorized();

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == auth0Id);

            if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.DevOpsPersonalAccessToken))
                return BadRequest(new { message = "Nemate podešen PAT token u podešavanjima." });

            // Parse Azure DevOps URL
            // Supported formats:
            //   https://dev.azure.com/{org}/{project}/_workitems/edit/{id}
            //   https://{org}.visualstudio.com/{project}/_workitems/edit/{id}
            if (!TryParseDevOpsUrl(dto.Url, out var organization, out var project, out var workItemId))
                return BadRequest(new { message = "Nevažeći Azure DevOps URL. Podržani format: https://dev.azure.com/{org}/{project}/_workitems/edit/{id}" });

            var apiUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}?$expand=all&api-version=7.1";

            var decryptedPat = _encryptionService.Decrypt(userSettings.DevOpsPersonalAccessToken);
            var client = _httpClientFactory.CreateClient();
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{decryptedPat}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ADO API returned {StatusCode} for work item {WorkItemId}: {Body}",
                    response.StatusCode, workItemId, errorBody);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return Unauthorized(new { message = "PAT token nije važeći ili je istekao." });

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound(new { message = $"Work item #{workItemId} nije pronađen u projektu '{project}'." });

                return StatusCode(502, new { message = "Greška pri komunikaciji sa Azure DevOps API-jem." });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var fields = doc.RootElement.GetProperty("fields");

            var result = new FetchedDevOpsTaskDto
            {
                DevOpsWorkItemId = workItemId,
                DevOpsUrl = dto.Url,
                Title = GetFieldString(fields, "System.Title"),
                Description = StripHtml(GetFieldString(fields, "System.Description")),
                AcceptanceCriteria = StripHtml(GetFieldString(fields, "Microsoft.VSTS.Common.AcceptanceCriteria")),
                Priority = MapPriority(fields),
                Estimation = MapEstimation(fields),
                WorkItemType = GetFieldString(fields, "System.WorkItemType"),
                DevOpsState = GetFieldString(fields, "System.State")
            };

            // Fetch status history if we have a candidateId
            if (dto.CandidateId.HasValue)
            {
                result.StatusHistory = await FetchAndSaveStatusHistoryAsync(
                    organization, project, workItemId, dto.CandidateId.Value, client, result.DevOpsState);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching work item from DevOps URL {Url}", dto.Url);
            return StatusCode(500, "Greška pri učitavanju taska iz Azure DevOps.");
        }
    }

    // POST: api/devopstaskscandidates/aktivnost/{aktivnostId}/refresh-from-devops
    [HttpPost("aktivnost/{aktivnostId}/refresh-from-devops")]
    public async Task<ActionResult<RefreshAktivnostDevOpsTasksResultDto>> RefreshAktivnostTasksFromDevOps(int aktivnostId)
    {
        try
        {
            var auth0Id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(auth0Id))
                return Unauthorized();

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == auth0Id);

            if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.DevOpsPersonalAccessToken))
                return BadRequest(new { message = "Nemate podešen PAT token u podešavanjima." });

            var candidates = await _context.DevOpsTasksCandidates
                .Where(t => t.AktivnostId == aktivnostId)
                .ToListAsync();

            var result = new RefreshAktivnostDevOpsTasksResultDto
            {
                TotalTasks = candidates.Count,
                SkippedTasks = candidates.Count(t => string.IsNullOrWhiteSpace(t.DevOpsUrl))
            };

            var decryptedPat = _encryptionService.Decrypt(userSettings.DevOpsPersonalAccessToken);
            var client = _httpClientFactory.CreateClient();
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{decryptedPat}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var candidate in candidates.Where(t => !string.IsNullOrWhiteSpace(t.DevOpsUrl)))
            {
                try
                {
                    if (!TryParseDevOpsUrl(candidate.DevOpsUrl!, out var organization, out var project, out var workItemId))
                    {
                        result.FailedTasks++;
                        continue;
                    }

                    var apiUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}?$expand=all&api-version=7.1";
                    var response = await client.GetAsync(apiUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        result.FailedTasks++;
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
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

                    var statusHistory = await FetchAndSaveStatusHistoryAsync(
                        organization, project, workItemId, candidate.Id, client, candidate.DevOpsState);

                    if (statusHistory.Any(h => h.IsActive && h.Status == "Ready"))
                        result.ReadyTasks++;

                    result.RefreshedTasks++;
                }
                catch (Exception ex)
                {
                    result.FailedTasks++;
                    _logger.LogWarning(ex, "Failed to refresh DevOps candidate {CandidateId}", candidate.Id);
                }
            }

            await _context.SaveChangesAsync();

            result.TotalTasks = await _context.DevOpsTasksCandidates
                .CountAsync(t => t.AktivnostId == aktivnostId);

            result.ReadyTasks = await _context.DevOpsTaskStatusHistory
                .Where(h => h.DurationMinutes == null
                    && h.Status == "Ready"
                    && h.DevOpsTasksCandidate!.AktivnostId == aktivnostId)
                .Select(h => h.DevOpsTaskCandidateId)
                .Distinct()
                .CountAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing DevOps tasks for aktivnost {AktivnostId}", aktivnostId);
            return StatusCode(500, "Greška pri osvežavanju taskova iz Azure DevOps.");
        }
    }

    private async Task<List<StatusHistoryEntryDto>> FetchAndSaveStatusHistoryAsync(
        string organization, string project, int workItemId, int candidateId, HttpClient client, string? fallbackState = null)
    {
        try
        {
            var updatesUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}/updates?api-version=7.1";
            var updatesResponse = await client.GetAsync(updatesUrl);
            if (!updatesResponse.IsSuccessStatusCode) return new List<StatusHistoryEntryDto>();

            var updatesJson = await updatesResponse.Content.ReadAsStringAsync();
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

                bool changed = false;

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
                    timeline.Add((revisedDate, currentState, currentAssignedTo));
            }

            var rawEntries = new List<(string Status, string? AssignedTo, DateTime StartedAt, DateTime? EndedAt, int? DurationMinutes)>();
            for (int i = 0; i < timeline.Count; i++)
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

            // Save raw entries to DB
            var existing = _context.DevOpsTaskStatusHistory.Where(h => h.DevOpsTaskCandidateId == candidateId);
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

            await _context.SaveChangesAsync();

            // Load role dictionary
            var roleDict = await _context.DevOpsUsers
                .Where(u => u.RoleId != null)
                .Join(_context.Codebooks, u => u.RoleId, cb => cb.Id, (u, cb) => new { u.DisplayName, cb.Value })
                .GroupBy(x => x.DisplayName)
                .Select(g => new { DisplayName = g.Key, RoleName = g.First().Value })
                .ToDictionaryAsync(x => x.DisplayName, x => x.RoleName);

            // Return grouped by (AssignedTo, Status)
            return rawEntries
                .GroupBy(e => new { e.AssignedTo, e.Status })
                .OrderByDescending(g => g.Max(e => e.StartedAt))
                .Select(g => new StatusHistoryEntryDto
                {
                    Status = g.Key.Status,
                    AssignedTo = g.Key.AssignedTo,
                    RoleName = g.Key.AssignedTo != null && roleDict.TryGetValue(g.Key.AssignedTo, out var r) ? r : null,
                    TotalDurationMinutes = g.Where(e => e.DurationMinutes.HasValue).Sum(e => (int?)e.DurationMinutes),
                    IsActive = g.Any(e => !e.DurationMinutes.HasValue),
                    StartedAt = g.Min(e => e.StartedAt),
                    EndedAt = g.Any(e => !e.EndedAt.HasValue) ? null : g.Max(e => e.EndedAt),
                    LastStartedAt = g.Max(e => e.StartedAt)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch status history for work item {WorkItemId}", workItemId);
            return new List<StatusHistoryEntryDto>();
        }
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

    // POST: api/devopstaskscandidates/sync-users
    [HttpPost("sync-users")]
    public async Task<ActionResult> SyncDevOpsUsers()
    {
        try
        {
            var auth0Id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(auth0Id)) return Unauthorized();

            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == auth0Id);

            if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.DevOpsPersonalAccessToken))
                return BadRequest(new { message = "Nemate podešen PAT token u podešavanjima." });

            var decryptedPat = _encryptionService.Decrypt(userSettings.DevOpsPersonalAccessToken);
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{decryptedPat}"));
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get all unique organizations from connected DevOps tasks
            var connectedUrls = await _context.DevOpsTasksCandidates
                .Where(t => t.DevOpsUrl != null && t.DevOpsUrl != "")
                .Select(t => t.DevOpsUrl!)
                .Distinct()
                .ToListAsync();

            var organizations = connectedUrls
                .Select(url =>
                {
                    TryParseDevOpsUrl(url, out var org, out _, out _);
                    return org;
                })
                .Where(org => !string.IsNullOrEmpty(org))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!organizations.Any())
                return BadRequest(new { message = "Nema konektovanih DevOps taskova na osnovu kojih bi se odredila organizacija." });

            // key = "principalName|organization"
            var collectedUsers = new Dictionary<string, DevOpsUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var org in organizations)
            {
                try
                {
                    // Use Graph API to enumerate all users in the organization (handles pagination via continuationToken)
                    string? continuationToken = null;
                    do
                    {
                        var graphUrl = $"https://vssps.dev.azure.com/{org}/_apis/graph/users?api-version=7.1-preview.1";
                        if (!string.IsNullOrEmpty(continuationToken))
                            graphUrl += $"&continuationToken={Uri.EscapeDataString(continuationToken)}";

                        var graphResponse = await client.GetAsync(graphUrl);
                        if (!graphResponse.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Graph users API failed for {Org}: {Status}", org, graphResponse.StatusCode);
                            break;
                        }

                        // Continuation token comes as a response header
                        continuationToken = null;
                        if (graphResponse.Headers.TryGetValues("X-MS-ContinuationToken", out var tokenValues))
                            continuationToken = tokenValues.FirstOrDefault();

                        var graphJson = await graphResponse.Content.ReadAsStringAsync();
                        using var graphDoc = JsonDocument.Parse(graphJson);

                        if (!graphDoc.RootElement.TryGetProperty("value", out var usersArray)) break;

                        foreach (var u in usersArray.EnumerateArray())
                        {
                            var subjectKind = u.TryGetProperty("subjectKind", out var sk) ? sk.GetString() : null;
                            // Only sync actual user accounts (not groups or service principals)
                            if (subjectKind != "user") continue;

                            var principalName = u.TryGetProperty("principalName", out var pn) ? pn.GetString() : null;
                            var displayName   = u.TryGetProperty("displayName",   out var dn) ? dn.GetString() : null;

                            if (string.IsNullOrWhiteSpace(principalName) || string.IsNullOrWhiteSpace(displayName))
                                continue;

                            // Skip service/build accounts
                            if (principalName.StartsWith("vstfs://", StringComparison.OrdinalIgnoreCase)) continue;

                            string? imageUrl = null;
                            if (u.TryGetProperty("_links", out var links) &&
                                links.TryGetProperty("avatar", out var avatar) &&
                                avatar.TryGetProperty("href", out var href))
                                imageUrl = href.GetString();

                            var key = $"{principalName}|{org}";
                            if (!collectedUsers.ContainsKey(key))
                            {
                                collectedUsers[key] = new DevOpsUser
                                {
                                    UniqueName   = principalName,
                                    DisplayName  = displayName,
                                    Organization = org!,
                                    ImageUrl     = imageUrl,
                                    SyncedAt     = DateTime.UtcNow
                                };
                            }
                        }
                    }
                    while (!string.IsNullOrEmpty(continuationToken));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync users from org {Org}", org);
                }
            }

            // Upsert into DevOpsUsers table
            int added = 0, updated = 0;
            foreach (var user in collectedUsers.Values)
            {
                var existing = await _context.DevOpsUsers
                    .FirstOrDefaultAsync(u => u.UniqueName == user.UniqueName && u.Organization == user.Organization);

                if (existing == null)
                {
                    _context.DevOpsUsers.Add(user);
                    added++;
                }
                else
                {
                    existing.DisplayName = user.DisplayName;
                    existing.ImageUrl = user.ImageUrl;
                    existing.SyncedAt = DateTime.UtcNow;
                    updated++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                added,
                updated,
                total = added + updated,
                message = $"Sinhronizovano {added + updated} korisnika ({added} novih, {updated} ažuriranih)."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing DevOps users");
            return StatusCode(500, "Greška pri sinhronizaciji korisnika.");
        }
    }

    // POST: api/devopstaskscandidates/sync-devops-now
    [HttpPost("sync-devops-now")]
    public async Task<ActionResult<DevOpsDailySyncResult>> SyncDevOpsTasksNow(CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            if (currentUser.Role != "Admin")
            {
                return Forbid();
            }

            var result = await _devOpsSyncService.RefreshAllLinkedTasksAsync(cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "DevOps sync je otkazan.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running manual DevOps task sync");
            return StatusCode(500, "Greška pri ručnom DevOps osvežavanju taskova.");
        }
    }

    private static string? GetIdentityFieldString(JsonElement identity, string property)
    {
        return identity.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }

    private static bool TryParseDevOpsUrl(string url, out string organization, out string project, out int workItemId)
    {
        organization = string.Empty;
        project = string.Empty;
        workItemId = 0;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath.TrimStart('/').TrimEnd('/').Split('/');

        // https://dev.azure.com/{org}/{project}/_workitems/edit/{id}
        if (uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length < 5) return false;
            organization = segments[0];
            project = segments[1];
            return int.TryParse(segments[^1], out workItemId);
        }

        // https://{org}.visualstudio.com/{project}/_workitems/edit/{id}
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
        // Try story points first, then original estimate
        if (fields.TryGetProperty("Microsoft.VSTS.Scheduling.StoryPoints", out var sp) && sp.ValueKind != JsonValueKind.Null)
            return $"{sp.GetDouble()} sp";

        if (fields.TryGetProperty("Microsoft.VSTS.Scheduling.OriginalEstimate", out var oe) && oe.ValueKind != JsonValueKind.Null)
            return $"{oe.GetDouble()} h";

        return null;
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;
        // Decode HTML entities only — keep HTML tags for frontend rendering
        return System.Net.WebUtility.HtmlDecode(html);
    }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class UpdateDevOpsTaskCandidateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? Priority { get; set; }
    public string? Estimation { get; set; }
    public int OrderIndex { get; set; }
    public string Status { get; set; } = "Draft";
}

public class CreateDevOpsTaskCandidateDto
{
    public int AktivnostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? Priority { get; set; }
    public string? Estimation { get; set; }
    public int OrderIndex { get; set; }
    public int? DevOpsWorkItemId { get; set; }
    public string? DevOpsUrl { get; set; }
}

public class FetchFromDevOpsUrlDto
{
    public string Url { get; set; } = string.Empty;
    /// <summary>
    /// Optional — if provided, status history will be fetched and saved.
    /// </summary>
    public int? CandidateId { get; set; }
}

public class FetchedDevOpsTaskDto
{
    public int DevOpsWorkItemId { get; set; }
    public string? DevOpsUrl { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? Priority { get; set; }
    public string? Estimation { get; set; }
    public string? WorkItemType { get; set; }
    public string? DevOpsState { get; set; }
    public List<StatusHistoryEntryDto> StatusHistory { get; set; } = new();
}

public class StatusHistoryEntryDto
{
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? RoleName { get; set; }
    public int? TotalDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? LastStartedAt { get; set; }
}

public class RefreshAktivnostDevOpsTasksResultDto
{
    public int TotalTasks { get; set; }
    public int ReadyTasks { get; set; }
    public int RefreshedTasks { get; set; }
    public int SkippedTasks { get; set; }
    public int FailedTasks { get; set; }
}

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

    public DevOpsTasksCandidatesController(
        ApplicationDbContext context,
        UserService userService,
        ILogger<DevOpsTasksCandidatesController> logger,
        IHttpClientFactory httpClientFactory,
        EncryptionService encryptionService)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
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
                User = c.User == null ? null : new
                {
                    c.User.Id,
                    c.User.Email,
                    c.User.Name
                },
                StatusHistory = (c.StatusHistory ?? Enumerable.Empty<DevOpsTaskStatusHistory>())
                    .GroupBy(h => new { h.AssignedTo, h.Status })
                    .OrderByDescending(g => g.Max(h => h.ChangedDate))
                    .Select(g => new
                    {
                        g.Key.Status,
                        g.Key.AssignedTo,
                        RoleName = g.Key.AssignedTo != null && roleDict.TryGetValue(g.Key.AssignedTo, out var r) ? r : null,
                        TotalDurationMinutes = g.Where(h => h.DurationMinutes.HasValue).Sum(h => h.DurationMinutes),
                        IsActive = g.Any(h => !h.DurationMinutes.HasValue),
                        LastStartedAt = g.Max(h => h.ChangedDate)
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
                WorkItemType = GetFieldString(fields, "System.WorkItemType")
            };

            // Fetch status history if we have a candidateId
            if (dto.CandidateId.HasValue)
            {
                result.StatusHistory = await FetchAndSaveStatusHistoryAsync(
                    organization, project, workItemId, dto.CandidateId.Value, client);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching work item from DevOps URL {Url}", dto.Url);
            return StatusCode(500, "Greška pri učitavanju taska iz Azure DevOps.");
        }
    }

    private async Task<List<StatusHistoryEntryDto>> FetchAndSaveStatusHistoryAsync(
        string organization, string project, int workItemId, int candidateId, HttpClient client)
    {
        try
        {
            var updatesUrl = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workitems/{workItemId}/updates?api-version=7.1";
            var updatesResponse = await client.GetAsync(updatesUrl);
            if (!updatesResponse.IsSuccessStatusCode) return new List<StatusHistoryEntryDto>();

            var updatesJson = await updatesResponse.Content.ReadAsStringAsync();
            using var updatesDoc = JsonDocument.Parse(updatesJson);

            // Build timeline: list of (changedDate, state, assignedTo) from each revision
            var timeline = new List<(DateTime ChangedDate, string? State, string? AssignedTo)>();
            string? currentState = null;
            string? currentAssignedTo = null;

            foreach (var update in updatesDoc.RootElement.GetProperty("value").EnumerateArray())
            {
                if (!update.TryGetProperty("fields", out var updFields)) continue;
                if (!update.TryGetProperty("revisedDate", out var revisedDateEl)) continue;
                if (!DateTime.TryParse(revisedDateEl.GetString(), out var revisedDate)) continue;

                bool changed = false;

                if (updFields.TryGetProperty("System.State", out var stateEl))
                {
                    var newState = stateEl.TryGetProperty("newValue", out var nv) && nv.ValueKind != JsonValueKind.Null
                        ? nv.GetString() : null;
                    if (newState != null && newState != currentState)
                    {
                        currentState = newState;
                        changed = true;
                    }
                }

                if (updFields.TryGetProperty("System.AssignedTo", out var assignedEl))
                {
                    string? newAssigned = null;
                    if (assignedEl.TryGetProperty("newValue", out var nv2) && nv2.ValueKind != JsonValueKind.Null)
                    {
                        newAssigned = nv2.ValueKind == JsonValueKind.Object && nv2.TryGetProperty("displayName", out var dn)
                            ? dn.GetString() : nv2.GetString();
                    }
                    if (newAssigned != currentAssignedTo)
                    {
                        currentAssignedTo = newAssigned;
                        changed = true;
                    }
                }

                if (changed && currentState != null)
                    timeline.Add((revisedDate, currentState, currentAssignedTo));
            }

            // Calculate duration for each raw entry
            var rawEntries = new List<(string Status, string? AssignedTo, DateTime ChangedDate, int? DurationMinutes)>();
            for (int i = 0; i < timeline.Count; i++)
            {
                var (changedDate, state, assignedTo) = timeline[i];
                int? durationMinutes = null;
                if (i + 1 < timeline.Count)
                    durationMinutes = (int)(timeline[i + 1].ChangedDate - changedDate).TotalMinutes;

                rawEntries.Add((state!, assignedTo, changedDate, durationMinutes));
            }

            // Save raw entries to DB
            var existing = _context.DevOpsTaskStatusHistory.Where(h => h.DevOpsTaskCandidateId == candidateId);
            _context.DevOpsTaskStatusHistory.RemoveRange(existing);

            _context.DevOpsTaskStatusHistory.AddRange(rawEntries.Select(e => new DevOpsTaskStatusHistory
            {
                DevOpsTaskCandidateId = candidateId,
                Status = e.Status,
                AssignedTo = e.AssignedTo,
                ChangedDate = e.ChangedDate,
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
                .OrderByDescending(g => g.Max(e => e.ChangedDate))
                .Select(g => new StatusHistoryEntryDto
                {
                    Status = g.Key.Status,
                    AssignedTo = g.Key.AssignedTo,
                    RoleName = g.Key.AssignedTo != null && roleDict.TryGetValue(g.Key.AssignedTo, out var r) ? r : null,
                    TotalDurationMinutes = g.Where(e => e.DurationMinutes.HasValue).Sum(e => (int?)e.DurationMinutes),
                    IsActive = g.Any(e => !e.DurationMinutes.HasValue),
                    LastStartedAt = g.Max(e => e.ChangedDate)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch status history for work item {WorkItemId}", workItemId);
            return new List<StatusHistoryEntryDto>();
        }
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

            // Get all unique org+project combos from connected DevOps tasks
            var connectedUrls = await _context.DevOpsTasksCandidates
                .Where(t => t.DevOpsUrl != null && t.DevOpsUrl != "")
                .Select(t => t.DevOpsUrl!)
                .Distinct()
                .ToListAsync();

            var devOpsProjects = connectedUrls
                .Select(url =>
                {
                    TryParseDevOpsUrl(url, out var org, out var proj, out _);
                    return new { DevOpsOrganization = org, DevOpsProject = proj };
                })
                .Where(x => !string.IsNullOrEmpty(x.DevOpsOrganization) && !string.IsNullOrEmpty(x.DevOpsProject))
                .DistinctBy(x => $"{x.DevOpsOrganization}|{x.DevOpsProject}")
                .ToList();

            if (!devOpsProjects.Any())
                return BadRequest(new { message = "Nema konektovanih DevOps taskova na osnovu kojih bi se odredila organizacija i projekat." });

            // key = "uniqueName|organization"
            var collectedUsers = new Dictionary<string, DevOpsUser>(StringComparer.OrdinalIgnoreCase);

            foreach (var proj in devOpsProjects)
            {
                try
                {
                    // WIQL - get all work item IDs in project
                    var wiqlUrl = $"https://dev.azure.com/{proj.DevOpsOrganization}/{proj.DevOpsProject}/_apis/wit/wiql?api-version=7.1";
                    var wiqlBody = JsonSerializer.Serialize(new { query = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{proj.DevOpsProject}'" });
                    var wiqlResponse = await client.PostAsync(wiqlUrl,
                        new StringContent(wiqlBody, Encoding.UTF8, "application/json"));

                    if (!wiqlResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("WIQL failed for {Org}/{Project}: {Status}", proj.DevOpsOrganization, proj.DevOpsProject, wiqlResponse.StatusCode);
                        continue;
                    }

                    var wiqlJson = await wiqlResponse.Content.ReadAsStringAsync();
                    using var wiqlDoc = JsonDocument.Parse(wiqlJson);
                    var ids = wiqlDoc.RootElement
                        .GetProperty("workItems")
                        .EnumerateArray()
                        .Select(wi => wi.GetProperty("id").GetInt32())
                        .ToList();

                    if (!ids.Any()) continue;

                    // Batch fetch user fields (max 200 per request)
                    for (int i = 0; i < ids.Count; i += 200)
                    {
                        var batch = ids.Skip(i).Take(200);
                        var idsParam = string.Join(",", batch);
                        var batchUrl = $"https://dev.azure.com/{proj.DevOpsOrganization}/{proj.DevOpsProject}/_apis/wit/workitems?ids={idsParam}&fields=System.AssignedTo,System.CreatedBy,System.ChangedBy&api-version=7.1";
                        var batchResponse = await client.GetAsync(batchUrl);
                        if (!batchResponse.IsSuccessStatusCode) continue;

                        var batchJson = await batchResponse.Content.ReadAsStringAsync();
                        using var batchDoc = JsonDocument.Parse(batchJson);

                        foreach (var item in batchDoc.RootElement.GetProperty("value").EnumerateArray())
                        {
                            if (!item.TryGetProperty("fields", out var fields)) continue;

                            foreach (var fieldName in new[] { "System.AssignedTo", "System.CreatedBy", "System.ChangedBy" })
                            {
                                if (!fields.TryGetProperty(fieldName, out var identity) ||
                                    identity.ValueKind != JsonValueKind.Object) continue;

                                var uniqueName = GetIdentityFieldString(identity, "uniqueName");
                                var displayName = GetIdentityFieldString(identity, "displayName");

                                if (string.IsNullOrWhiteSpace(uniqueName) || string.IsNullOrWhiteSpace(displayName))
                                    continue;

                                var key = $"{uniqueName}|{proj.DevOpsOrganization}";
                                if (!collectedUsers.ContainsKey(key))
                                {
                                    collectedUsers[key] = new DevOpsUser
                                    {
                                        UniqueName = uniqueName,
                                        DisplayName = displayName,
                                        Organization = proj.DevOpsOrganization!,
                                        ImageUrl = GetIdentityFieldString(identity, "imageUrl"),
                                        SyncedAt = DateTime.UtcNow
                                    };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync users from {Org}/{Project}", proj.DevOpsOrganization, proj.DevOpsProject);
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
    public List<StatusHistoryEntryDto> StatusHistory { get; set; } = new();
}

public class StatusHistoryEntryDto
{
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? RoleName { get; set; }
    public int? TotalDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastStartedAt { get; set; }
}

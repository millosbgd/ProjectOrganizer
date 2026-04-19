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

            var candidates = await _context.DevOpsTasksCandidates
                .Include(c => c.User)
                .Where(c => c.AktivnostId == aktivnostId)
                .OrderBy(c => c.OrderIndex)
                .ThenByDescending(c => c.CreatedAt)
                .Select(c => new
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
                    User = new
                    {
                        c.User!.Id,
                        c.User.Email,
                        c.User.Name
                    }
                })
                .ToListAsync();

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

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching work item from DevOps URL {Url}", dto.Url);
            return StatusCode(500, "Greška pri učitavanju taska iz Azure DevOps.");
        }
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
        // Remove HTML tags using regex
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*(>|$)", string.Empty).Trim();
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
}

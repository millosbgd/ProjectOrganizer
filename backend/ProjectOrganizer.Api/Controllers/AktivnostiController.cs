using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;
using System.Security.Claims;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AktivnostiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AktivnostiController> _logger;
    private readonly OpenAIService _openAIService;
    private readonly UserService _userService;

    public AktivnostiController(
        ApplicationDbContext context, 
        ILogger<AktivnostiController> logger,
        OpenAIService openAIService,
        UserService userService)
    {
        _context = context;
        _logger = logger;
        _openAIService = openAIService;
        _userService = userService;
    }

    // GET: api/Aktivnosti
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Aktivnost>>> GetAktivnosti(
        [FromQuery] int? projekatId = null, 
        [FromQuery] bool myActivitiesOnly = false)
    {
        var query = _context.Aktivnosti
            .Include(a => a.Projekat)
            .Include(a => a.CreatedByUser)
            .AsQueryable();

        if (projekatId.HasValue)
            query = query.Where(a => a.ProjekatId == projekatId.Value);

        // Filter by current user if requested (default is to show only user's activities)
        if (myActivitiesOnly)
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            query = query.Where(a => a.CreatedBy == currentUser.Id);
        }

        var aktivnosti = await query
            .OrderByDescending(a => a.Datum)
            .ToListAsync();

        return Ok(aktivnosti);
    }

    // GET: api/Aktivnosti/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Aktivnost>> GetAktivnost(int id)
    {
        var aktivnost = await _context.Aktivnosti
            .Include(a => a.Projekat)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aktivnost == null)
            return NotFound();

        return Ok(aktivnost);
    }

    // POST: api/Aktivnosti
    [HttpPost]
    public async Task<ActionResult<Aktivnost>> CreateAktivnost(Aktivnost aktivnost)
    {
        // BAU activities don't need a project
        if (aktivnost.Bau)
        {
            aktivnost.ProjekatId = null;
        }
        else
        {
            // Check if Projekat exists for non-BAU activities
            if (aktivnost.ProjekatId == null || !await _context.Projekti.AnyAsync(p => p.Id == aktivnost.ProjekatId))
                return BadRequest("Projekat ne postoji.");
        }

        // Get current user and set as creator
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        aktivnost.CreatedBy = currentUser.Id;

        aktivnost.CreatedAt = DateTime.UtcNow;
        aktivnost.UpdatedAt = DateTime.UtcNow;

        _context.Aktivnosti.Add(aktivnost);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAktivnost), new { id = aktivnost.Id }, aktivnost);
    }

    // PUT: api/Aktivnosti/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAktivnost(int id, Aktivnost aktivnost)
    {
        if (id != aktivnost.Id)
            return BadRequest();

        var existingAktivnost = await _context.Aktivnosti.FindAsync(id);
        if (existingAktivnost == null)
            return NotFound();

        existingAktivnost.Opis = aktivnost.Opis;
        existingAktivnost.Detalji = aktivnost.Detalji;
        existingAktivnost.Datum = aktivnost.Datum;
        existingAktivnost.StartUtc = aktivnost.StartUtc;
        existingAktivnost.EndUtc = aktivnost.EndUtc;
        existingAktivnost.Status = aktivnost.Status;
        existingAktivnost.Vrsta = aktivnost.Vrsta;
        existingAktivnost.ProjekatId = aktivnost.ProjekatId;
        existingAktivnost.ProjectImplementationItemId = aktivnost.ProjectImplementationItemId;
        existingAktivnost.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Aktivnosti.AnyAsync(a => a.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Aktivnosti/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAktivnost(int id)
    {
        var aktivnost = await _context.Aktivnosti.FindAsync(id);
        if (aktivnost == null)
            return NotFound();

        // Check if activity is linked to an implementation item
        if (aktivnost.ProjectImplementationItemId.HasValue && aktivnost.ProjectImplementationItemId.Value > 0)
        {
            return BadRequest(new { message = "Ne možete obrisati aktivnost koja je vezana za stavku implementacije." });
        }

        // Check if activity has saved DevOps tasks
        var hasTasks = await _context.DevOpsTasksCandidates
            .AnyAsync(t => t.AktivnostId == id);
        
        if (hasTasks)
        {
            return BadRequest(new { message = "Ne možete obrisati aktivnost koja ima sačuvane DevOps taskove." });
        }

        _context.Aktivnosti.Remove(aktivnost);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/Aktivnosti/5/generate-zapisnik
    [HttpPost("{id}/generate-zapisnik")]
    public async Task<ActionResult<string>> GenerateZapisnik(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get user settings
        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
            return BadRequest("Morate prvo konfigurisati OpenAI API ključ u podešavanjima.");

        var aktivnost = await _context.Aktivnosti
            .Include(a => a.Projekat)
                .ThenInclude(p => p.Klijent)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aktivnost == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(aktivnost.Detalji))
            return BadRequest("Aktivnost nema detalje za generisanje zapisnika.");

        try
        {
            var zapisnik = await _openAIService.GenerateZapisnikAsync(
                userSettings.OpenAiApiKey,
                userSettings.OpenAiModel,
                aktivnost.Projekat.Klijent.Naziv,
                aktivnost.Projekat.Naziv,
                aktivnost.Datum,
                aktivnost.Vrsta,
                aktivnost.Status,
                aktivnost.Opis,
                aktivnost.Detalji
            );

            return Ok(zapisnik);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating zapisnik for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom generisanja zapisnika.");
        }
    }

    // POST: api/Aktivnosti/{id}/generate-devops-tasks
    [HttpPost("{id}/generate-devops-tasks")]
    public async Task<ActionResult<string>> GenerateDevOpsTasks(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var userSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (userSettings == null || string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
            return BadRequest("Morate prvo konfigurisati OpenAI API ključ u podešavanjima.");

        var aktivnost = await _context.Aktivnosti
            .Include(a => a.Projekat)
                .ThenInclude(p => p.Klijent)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aktivnost == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(aktivnost.Detalji))
            return BadRequest("Aktivnost nema detalje za generisanje taskova.");

        try
        {
            var tasks = await _openAIService.GenerateDevOpsTasks(
                userSettings.OpenAiApiKey,
                userSettings.OpenAiModel,
                aktivnost.Projekat.Klijent.Naziv,
                aktivnost.Projekat.Naziv,
                aktivnost.Opis,
                aktivnost.Detalji
            );

            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DevOps tasks for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom generisanja taskova.");
        }
    }

    // POST: api/Aktivnosti/{id}/parse-devops-tasks
    [HttpPost("{id}/parse-devops-tasks")]
    public async Task<ActionResult<List<object>>> ParseDevOpsTasksFromText(int id, [FromBody] ParseTasksRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var parsedTasks = ParseDevOpsTasksToObjects(request.TasksText);
            return Ok(parsedTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DevOps tasks for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom parsiranja taskova.");
        }
    }

    // POST: api/Aktivnosti/{id}/save-selected-tasks
    [HttpPost("{id}/save-selected-tasks")]
    public async Task<ActionResult> SaveSelectedTasks(int id, [FromBody] SaveTasksRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Auth0Id == userId);

            if (currentUser == null)
                return Unauthorized();

            var candidates = new List<DevOpsTasksCandidate>();
            
            foreach (var task in request.Tasks)
            {
                var candidate = new DevOpsTasksCandidate
                {
                    AktivnostId = id,
                    UserId = currentUser.Id,
                    Title = task.Title,
                    Description = task.Description,
                    AcceptanceCriteria = task.AcceptanceCriteria,
                    Priority = task.Priority,
                    Estimation = task.Estimation,
                    OrderIndex = task.OrderIndex,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow
                };
                
                candidates.Add(candidate);
            }

            _context.DevOpsTasksCandidates.AddRange(candidates);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Taskovi su uspešno sačuvani.", count = candidates.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving selected tasks for aktivnost {AktivnostId}", id);
            return StatusCode(500, "Greška prilikom čuvanja taskova.");
        }
    }

    private List<object> ParseDevOpsTasksToObjects(string tasksText)
    {
        var tasks = new List<object>();
        var taskSections = System.Text.RegularExpressions.Regex.Split(tasksText, @"\[TASK \d+\]");
        
        int orderIndex = 1;
        foreach (var section in taskSections)
        {
            if (string.IsNullOrWhiteSpace(section))
                continue;

            var task = new 
            {
                OrderIndex = orderIndex++,
                Title = ExtractField(section, @"Naziv:\s*(.+?)(?:\r?\n|$)"),
                Description = ExtractField(section, @"Opis:\s*(.+?)(?=Acceptance Criteria:|Prioritet:|$)", singleLine: false),
                AcceptanceCriteria = ExtractField(section, @"Acceptance Criteria:\s*(.+?)(?=Prioritet:|Procena:|$)", singleLine: false),
                Priority = ExtractField(section, @"Prioritet:\s*(.+?)(?:\r?\n|$)"),
                Estimation = ExtractField(section, @"Procena:\s*(.+?)(?:\r?\n|$)")
            };

            if (!string.IsNullOrWhiteSpace(task.Title))
                tasks.Add(task);
        }

        return tasks;
    }

    private string ExtractField(string section, string pattern, bool singleLine = true)
    {
        var options = singleLine ? System.Text.RegularExpressions.RegexOptions.Multiline : System.Text.RegularExpressions.RegexOptions.Singleline;
        var match = System.Text.RegularExpressions.Regex.Match(section, pattern, options);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private List<DevOpsTasksCandidate> ParseDevOpsTasks(string tasksText, int aktivnostId, int userId)
    {
        var candidates = new List<DevOpsTasksCandidate>();
        var taskSections = System.Text.RegularExpressions.Regex.Split(tasksText, @"\[TASK \d+\]");
        
        int orderIndex = 1;
        foreach (var section in taskSections)
        {
            if (string.IsNullOrWhiteSpace(section))
                continue;

            var candidate = new DevOpsTasksCandidate
            {
                AktivnostId = aktivnostId,
                UserId = userId,
                OrderIndex = orderIndex++,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            // Extract Title
            var titleMatch = System.Text.RegularExpressions.Regex.Match(section, @"Naziv:\s*(.+?)(?:\r?\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (titleMatch.Success)
                candidate.Title = titleMatch.Groups[1].Value.Trim();

            // Extract Description
            var descMatch = System.Text.RegularExpressions.Regex.Match(section, @"Opis:\s*(.+?)(?=Acceptance Criteria:|Prioritet:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (descMatch.Success)
                candidate.Description = descMatch.Groups[1].Value.Trim();

            // Extract Acceptance Criteria
            var criteriaMatch = System.Text.RegularExpressions.Regex.Match(section, @"Acceptance Criteria:\s*(.+?)(?=Prioritet:|Procena:|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (criteriaMatch.Success)
                candidate.AcceptanceCriteria = criteriaMatch.Groups[1].Value.Trim();

            // Extract Priority
            var priorityMatch = System.Text.RegularExpressions.Regex.Match(section, @"Prioritet:\s*(.+?)(?:\r?\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (priorityMatch.Success)
                candidate.Priority = priorityMatch.Groups[1].Value.Trim();

            // Extract Estimation
            var estimationMatch = System.Text.RegularExpressions.Regex.Match(section, @"Procena:\s*(.+?)(?:\r?\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (estimationMatch.Success)
                candidate.Estimation = estimationMatch.Groups[1].Value.Trim();

            if (!string.IsNullOrWhiteSpace(candidate.Title))
                candidates.Add(candidate);
        }

        return candidates;
    }
}

// DTOs for new endpoints
public class ParseTasksRequest
{
    public string TasksText { get; set; } = string.Empty;
}

public class SaveTasksRequest
{
    public List<TaskDto> Tasks { get; set; } = new();
}

public class TaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public string? Priority { get; set; }
    public string? Estimation { get; set; }
    public int OrderIndex { get; set; }
}

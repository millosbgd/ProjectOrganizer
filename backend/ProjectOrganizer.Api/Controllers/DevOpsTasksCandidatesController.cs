using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevOpsTasksCandidatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<DevOpsTasksCandidatesController> _logger;

    public DevOpsTasksCandidatesController(
        ApplicationDbContext context,
        UserService userService,
        ILogger<DevOpsTasksCandidatesController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
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

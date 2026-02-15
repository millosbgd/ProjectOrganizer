using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using System.Security.Claims;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(
        ApplicationDbContext context,
        ILogger<ActivitiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get activities for calendar view filtered by date range
    /// </summary>
    /// <param name="from">Start date in ISO format (UTC)</param>
    /// <param name="to">End date in ISO format (UTC)</param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarActivityDto>>> GetActivities(
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            return BadRequest("Parameters 'from' and 'to' are required.");
        }

        if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
        {
            return BadRequest("Invalid date format. Use ISO format (UTC).");
        }

        // Query activities that overlap with the requested range
        // (StartUtc < to && EndUtc > from)
        var activities = await _context.Aktivnosti
            .Include(a => a.Projekat)
            .Where(a => a.StartUtc.HasValue && a.EndUtc.HasValue && a.StartUtc < toDate && a.EndUtc > fromDate)
            .OrderBy(a => a.StartUtc)
            .Select(a => new CalendarActivityDto
            {
                Id = a.Id,
                Title = a.Opis,
                Start = a.StartUtc!.Value,
                End = a.EndUtc!.Value,
                Type = a.Vrsta,
                ProjectName = a.Projekat != null ? a.Projekat.Naziv : "Unknown",
                ProjectId = a.ProjekatId
            })
            .ToListAsync();

        return Ok(activities);
    }

    /// <summary>
    /// Update activity start and end time (for drag & drop and resize)
    /// </summary>
    /// <param name="id">Activity ID</param>
    /// <param name="dto">Update DTO with new times</param>
    [HttpPatch("{id}/time")]
    public async Task<IActionResult> UpdateActivityTime(int id, [FromBody] UpdateActivityTimeDto dto)
    {
        var activity = await _context.Aktivnosti.FindAsync(id);
        
        if (activity == null)
        {
            return NotFound(new { message = "Aktivnost nije pronađena." });
        }

        // Validate that end is after start
        if (dto.EndUtc <= dto.StartUtc)
        {
            return BadRequest(new { message = "Vreme završetka mora biti posle vremena početka." });
        }

        // Update times
        activity.StartUtc = dto.StartUtc;
        activity.EndUtc = dto.EndUtc;
        activity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Aktivnosti.AnyAsync(e => e.Id == id))
            {
                return NotFound(new { message = "Aktivnost nije pronađena." });
            }
            throw;
        }
    }
}

/// <summary>
/// DTO for calendar activities
/// </summary>
public class CalendarActivityDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int ProjectId { get; set; }
}

/// <summary>
/// DTO for updating activity time
/// </summary>
public class UpdateActivityTimeDto
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}

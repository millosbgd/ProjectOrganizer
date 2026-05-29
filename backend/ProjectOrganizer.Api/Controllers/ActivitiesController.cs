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
public class ActivitiesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ActivitiesController> _logger;
    private readonly UserService _userService;

    public ActivitiesController(
        ApplicationDbContext context,
        ILogger<ActivitiesController> logger,
        UserService userService)
    {
        _context = context;
        _logger = logger;
        _userService = userService;
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

        // Get current user
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        // Query activities that overlap with the requested range and belong to the current user
        // (StartUtc < to && EndUtc > from && CreatedBy == currentUserId)
        var activities = await _context.Aktivnosti
            .Include(a => a.Projekat)
            .Where(a => a.StartUtc.HasValue && a.EndUtc.HasValue 
                && a.StartUtc < toDate && a.EndUtc > fromDate
                && a.CreatedBy == currentUser.Id)
            .OrderBy(a => a.StartUtc)
            .Select(a => new CalendarActivityDto
            {
                Id = a.Id,
                Title = a.Opis,
                Start = a.StartUtc!.Value,
                End = a.EndUtc!.Value,
                Type = a.Vrsta,
                ProjectName = a.Projekat != null ? a.Projekat.Naziv : "BAU",
                ProjectId = a.ProjekatId,
                Bau = a.Bau
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
        // Get current user
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var activity = await _context.Aktivnosti.FindAsync(id);
        
        if (activity == null)
        {
            return NotFound(new { message = "Aktivnost nije pronađena." });
        }

        // Check if user owns this activity
        if (activity.CreatedBy != currentUser.Id)
        {
            return Forbid();
        }

        // Validate that end is after start
        if (dto.EndUtc <= dto.StartUtc)
        {
            return BadRequest(new { message = "Vreme završetka mora biti posle vremena početka." });
        }

        // Update times and date field
        activity.StartUtc = dto.StartUtc;
        activity.EndUtc = dto.EndUtc;
        activity.Datum = dto.StartUtc; // Update Datum field based on activity start time
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

    /// <summary>
    /// Schedule BAU activities for a selected day into the 08:00-16:00 work window.
    /// Non-BAU activities are treated as fixed intervals and are never moved.
    /// </summary>
    [HttpPost("schedule-bau-day")]
    public async Task<ActionResult<ScheduleBauDayResultDto>> ScheduleBauDay([FromBody] ScheduleBauDayRequestDto dto)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        var timeZone = ResolveBelgradeTimeZone();
        var localDay = dto.Datum.Date;

        var workStartLocal = localDay.AddHours(8);
        var workEndLocal = localDay.AddHours(16);
        var localNextDay = localDay.AddDays(1);
        var workStartUtc = ConvertLocalToUtc(workStartLocal, timeZone);
        var workEndUtc = ConvertLocalToUtc(workEndLocal, timeZone);

        var fixedActivities = await _context.Aktivnosti
            .Where(a => a.CreatedBy == currentUser.Id
                && !a.Bau
                && a.StartUtc.HasValue
                && a.EndUtc.HasValue
                && a.StartUtc < workEndUtc
                && a.EndUtc > workStartUtc)
            .OrderBy(a => a.StartUtc)
            .ToListAsync();

        var busyIntervals = fixedActivities
            .Select(a => (
                Start: a.StartUtc!.Value < workStartUtc ? workStartUtc : a.StartUtc.Value,
                End: a.EndUtc!.Value > workEndUtc ? workEndUtc : a.EndUtc.Value
            ))
            .Where(i => i.End > i.Start)
            .OrderBy(i => i.Start)
            .ToList();

        var freeSlots = BuildFreeSlots(workStartUtc, workEndUtc, busyIntervals);

        var bauActivities = await _context.Aktivnosti
            .Where(a => a.CreatedBy == currentUser.Id
                && a.Bau
                && a.Datum >= localDay
                && a.Datum < localNextDay)
            .ToListAsync();

        if (bauActivities.Count == 0)
            return BadRequest(new { message = "Nema BAU aktivnosti za izabrani dan." });

        var invalidDuration = bauActivities.FirstOrDefault(a => !a.BauTrajanjeMinuta.HasValue || a.BauTrajanjeMinuta <= 0);
        if (invalidDuration != null)
            return BadRequest(new { message = $"BAU aktivnost #{invalidDuration.Id} nema validno trajanje." });

        var totalBauMinutes = bauActivities.Sum(a => a.BauTrajanjeMinuta!.Value);
        var totalFreeMinutes = freeSlots.Sum(s => (int)(s.End - s.Start).TotalMinutes);

        if (totalFreeMinutes < bauActivities.Count)
        {
            return BadRequest(new
            {
                message = $"Nema dovoljno slobodnog vremena za sve BAU aktivnosti. Slobodno: {totalFreeMinutes} min, aktivnosti: {bauActivities.Count}."
            });
        }

        var slots = freeSlots.ToList();
        var scheduled = new List<ScheduleBauActivityDto>();
        var scaledDurations = ScaleDurationsToAvailableTime(
            bauActivities.Select(a => (Activity: a, RequestedMinutes: a.BauTrajanjeMinuta!.Value)).ToList(),
            totalFreeMinutes);

        foreach (var item in scaledDurations
            .OrderByDescending(i => i.ScheduledMinutes)
            .ThenBy(i => i.Activity.CreatedAt)
            .ThenBy(i => i.Activity.Id))
        {
            var activity = item.Activity;
            var duration = TimeSpan.FromMinutes(item.ScheduledMinutes);
            var slotIndex = slots.FindIndex(s => s.End - s.Start >= duration);

            if (slotIndex < 0)
            {
                return BadRequest(new { message = "BAU aktivnosti ne mogu da se uklope u postojeće slobodne vremenske slotove." });
            }

            var slot = slots[slotIndex];
            var start = slot.Start;
            var end = start.Add(duration);

            activity.StartUtc = start;
            activity.EndUtc = end;
            activity.UpdatedAt = DateTime.UtcNow;

            scheduled.Add(new ScheduleBauActivityDto
            {
                Id = activity.Id,
                StartUtc = start,
                EndUtc = end,
                RequestedDurationMinutes = item.RequestedMinutes,
                DurationMinutes = item.ScheduledMinutes
            });

            slots[slotIndex] = (end, slot.End);
            if (slots[slotIndex].End <= slots[slotIndex].Start)
                slots.RemoveAt(slotIndex);
        }

        await _context.SaveChangesAsync();

        return Ok(new ScheduleBauDayResultDto
        {
            ScheduledCount = scheduled.Count,
            TotalBauMinutes = totalBauMinutes,
            ScheduledBauMinutes = scheduled.Sum(a => a.DurationMinutes),
            WasScaled = totalBauMinutes > totalFreeMinutes,
            FixedActivityCount = fixedActivities.Count,
            Activities = scheduled.OrderBy(a => a.StartUtc).ToList()
        });
    }

    private static List<ScaledBauDuration> ScaleDurationsToAvailableTime(
        List<(Aktivnost Activity, int RequestedMinutes)> activities,
        int totalFreeMinutes)
    {
        var requestedTotal = activities.Sum(a => a.RequestedMinutes);
        if (requestedTotal <= totalFreeMinutes)
        {
            return activities
                .Select(a => new ScaledBauDuration(a.Activity, a.RequestedMinutes, a.RequestedMinutes))
                .ToList();
        }

        var scaled = activities
            .Select(a =>
            {
                var exact = (decimal)a.RequestedMinutes * totalFreeMinutes / requestedTotal;
                var scheduled = Math.Max(1, (int)Math.Floor(exact));
                return new
                {
                    a.Activity,
                    a.RequestedMinutes,
                    ScheduledMinutes = scheduled,
                    Remainder = exact - scheduled
                };
            })
            .ToList();

        var remainingMinutes = totalFreeMinutes - scaled.Sum(a => a.ScheduledMinutes);
        var orderedForRemainder = scaled
            .OrderByDescending(a => a.Remainder)
            .ThenByDescending(a => a.RequestedMinutes)
            .ThenBy(a => a.Activity.CreatedAt)
            .ThenBy(a => a.Activity.Id)
            .ToList();

        var extraByActivityId = orderedForRemainder.ToDictionary(a => a.Activity.Id, _ => 0);
        for (var i = 0; i < remainingMinutes; i++)
        {
            var item = orderedForRemainder[i % orderedForRemainder.Count];
            extraByActivityId[item.Activity.Id]++;
        }

        return scaled
            .Select(a => new ScaledBauDuration(
                a.Activity,
                a.RequestedMinutes,
                a.ScheduledMinutes + extraByActivityId[a.Activity.Id]))
            .ToList();
    }

    private static List<(DateTime Start, DateTime End)> BuildFreeSlots(
        DateTime workStartUtc,
        DateTime workEndUtc,
        List<(DateTime Start, DateTime End)> busyIntervals)
    {
        var freeSlots = new List<(DateTime Start, DateTime End)>();
        var cursor = workStartUtc;

        foreach (var interval in MergeIntervals(busyIntervals))
        {
            if (interval.Start > cursor)
                freeSlots.Add((cursor, interval.Start));

            if (interval.End > cursor)
                cursor = interval.End;
        }

        if (cursor < workEndUtc)
            freeSlots.Add((cursor, workEndUtc));

        return freeSlots;
    }

    private static List<(DateTime Start, DateTime End)> MergeIntervals(List<(DateTime Start, DateTime End)> intervals)
    {
        var merged = new List<(DateTime Start, DateTime End)>();
        foreach (var interval in intervals.OrderBy(i => i.Start))
        {
            if (merged.Count == 0 || interval.Start > merged[^1].End)
            {
                merged.Add(interval);
                continue;
            }

            if (interval.End > merged[^1].End)
                merged[^1] = (merged[^1].Start, interval.End);
        }

        return merged;
    }

    private static DateTime ConvertLocalToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }

    private static TimeZoneInfo ResolveBelgradeTimeZone()
    {
        foreach (var id in new[] { "Europe/Belgrade", "Central European Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
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
    public int? ProjectId { get; set; }
    public bool Bau { get; set; }
}

/// <summary>
/// DTO for updating activity time
/// </summary>
public class UpdateActivityTimeDto
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}

public class ScheduleBauDayRequestDto
{
    public DateTime Datum { get; set; }
}

public class ScheduleBauDayResultDto
{
    public int ScheduledCount { get; set; }
    public int TotalBauMinutes { get; set; }
    public int ScheduledBauMinutes { get; set; }
    public bool WasScaled { get; set; }
    public int FixedActivityCount { get; set; }
    public List<ScheduleBauActivityDto> Activities { get; set; } = new();
}

public class ScheduleBauActivityDto
{
    public int Id { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public int RequestedDurationMinutes { get; set; }
    public int DurationMinutes { get; set; }
}

public record ScaledBauDuration(Aktivnost Activity, int RequestedMinutes, int ScheduledMinutes);

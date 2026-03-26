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
public class DailyTasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;

    public DailyTasksController(ApplicationDbContext context, UserService userService)
    {
        _context = context;
        _userService = userService;
    }

    // GET: api/DailyTasks
    // Vraća taskove iz poslednjih 4 dana + sve Pinned taskove korisnika
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)); // today - 3 = 4 days window

        var tasks = await _context.DailyTasks
            .Where(t => t.KorisnikKreirao == currentUser.Id
                        && (t.Datum >= cutoffDate || t.Pinned))
            .OrderByDescending(t => t.Pinned)
            .ThenByDescending(t => t.Datum)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return Ok(tasks);
    }

    // POST: api/DailyTasks
    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateDailyTaskDto dto)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var task = new DailyTask
        {
            Datum = DateOnly.FromDateTime(DateTime.Today),
            Opis = dto.Opis.Trim(),
            KorisnikKreirao = currentUser.Id,
            Solved = false,
            Pinned = false
        };

        _context.DailyTasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
    }

    // PUT: api/DailyTasks/{id}/solved
    [HttpPut("{id:int}/solved")]
    public async Task<IActionResult> ToggleSolved(int id)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var task = await _context.DailyTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.KorisnikKreirao == currentUser.Id);

        if (task == null) return NotFound();

        task.Solved = !task.Solved;
        await _context.SaveChangesAsync();

        return Ok(task);
    }

    // PUT: api/DailyTasks/{id}/pinned
    [HttpPut("{id:int}/pinned")]
    public async Task<IActionResult> TogglePinned(int id)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var task = await _context.DailyTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.KorisnikKreirao == currentUser.Id);

        if (task == null) return NotFound();

        task.Pinned = !task.Pinned;
        await _context.SaveChangesAsync();

        return Ok(task);
    }

    // DELETE: api/DailyTasks/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var task = await _context.DailyTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.KorisnikKreirao == currentUser.Id);

        if (task == null) return NotFound();

        _context.DailyTasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

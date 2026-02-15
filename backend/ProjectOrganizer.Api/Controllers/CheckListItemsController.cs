using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckListItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckListItemsController> _logger;

    public CheckListItemsController(
        ApplicationDbContext context,
        ILogger<CheckListItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all check list items (codebook)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CheckListItem>>> GetCheckListItems()
    {
        var items = await _context.CheckListItems
            .OrderBy(c => c.Opis)
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Get check list item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CheckListItem>> GetCheckListItem(int id)
    {
        var item = await _context.CheckListItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = "Stavka čekliste nije pronađena." });
        }

        return Ok(item);
    }

    /// <summary>
    /// Create new check list item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CheckListItem>> CreateCheckListItem(CheckListItem item)
    {
        _context.CheckListItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCheckListItem), new { id = item.Id }, item);
    }

    /// <summary>
    /// Update check list item
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCheckListItem(int id, CheckListItem item)
    {
        if (id != item.Id)
        {
            return BadRequest(new { message = "ID ne odgovara." });
        }

        _context.Entry(item).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.CheckListItems.AnyAsync(e => e.Id == id))
            {
                return NotFound(new { message = "Stavka čekliste nije pronađena." });
            }
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Delete check list item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCheckListItem(int id)
    {
        var item = await _context.CheckListItems.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = "Stavka čekliste nije pronađena." });
        }

        // Check if it's used in any implementation items
        var isUsed = await _context.ImplementationItemCheckListItems
            .AnyAsync(ic => ic.CheckListItemId == id);

        if (isUsed)
        {
            return BadRequest(new { message = "Stavka čekliste se koristi i ne može biti obrisana." });
        }

        _context.CheckListItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

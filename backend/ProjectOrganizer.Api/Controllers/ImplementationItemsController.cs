using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImplementationItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImplementationItemsController> _logger;

    public ImplementationItemsController(
        ApplicationDbContext context,
        ILogger<ImplementationItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get implementation item by ID with check list items
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ImplementationItemDto>> GetImplementationItem(int id)
    {
        var item = await _context.ImplementationItems
            .Include(i => i.CheckListItems)
                .ThenInclude(ic => ic.CheckListItem)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound(new { message = "Stavka modela nije pronađena." });
        }

        var dto = new ImplementationItemDto
        {
            Id = item.Id,
            ImplementationModelId = item.ImplementationModelId,
            Naziv = item.Naziv,
            Detalji = item.Detalji,
            CheckListItems = item.CheckListItems.Select(ic => new CheckListItemDto
            {
                Id = ic.CheckListItem!.Id,
                Opis = ic.CheckListItem.Opis,
                Kompleksnost = ic.CheckListItem.Kompleksnost
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Get check list items for implementation item
    /// </summary>
    [HttpGet("{id}/checklists")]
    public async Task<ActionResult<IEnumerable<CheckListItemDto>>> GetImplementationItemCheckLists(int id)
    {
        var exists = await _context.ImplementationItems.AnyAsync(i => i.Id == id);
        if (!exists)
        {
            return NotFound(new { message = "Stavka modela nije pronađena." });
        }

        var checkLists = await _context.ImplementationItemCheckListItems
            .Where(ic => ic.ImplementationItemId == id)
            .Include(ic => ic.CheckListItem)
            .Select(ic => new CheckListItemDto
            {
                Id = ic.CheckListItem!.Id,
                Opis = ic.CheckListItem.Opis,
                Kompleksnost = ic.CheckListItem.Kompleksnost,
                LinkId = ic.Id  // ID of the junction table record for deletion
            })
            .OrderBy(c => c.Opis)
            .ToListAsync();

        return Ok(checkLists);
    }

    /// <summary>
    /// Add check list items to implementation item
    /// </summary>
    [HttpPost("{id}/checklists")]
    public async Task<IActionResult> AddCheckListItems(int id, [FromBody] AddCheckListItemsDto dto)
    {
        var item = await _context.ImplementationItems.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = "Stavka modela nije pronađena." });
        }

        // Get existing check list items for this implementation item
        var existingCheckListIds = await _context.ImplementationItemCheckListItems
            .Where(ic => ic.ImplementationItemId == id)
            .Select(ic => ic.CheckListItemId)
            .ToListAsync();

        // Add only new check list items (avoid duplicates)
        var newCheckListIds = dto.CheckListItemIds
            .Where(cid => !existingCheckListIds.Contains(cid))
            .ToList();

        if (newCheckListIds.Count == 0)
        {
            return Ok(new { message = "Sve stavke su već dodane." });
        }

        // Verify that all check list items exist
        var checkListItems = await _context.CheckListItems
            .Where(c => newCheckListIds.Contains(c.Id))
            .ToListAsync();

        if (checkListItems.Count != newCheckListIds.Count)
        {
            return BadRequest(new { message = "Neke stavke čekliste ne postoje." });
        }

        // Create junction records
        foreach (var checkListId in newCheckListIds)
        {
            _context.ImplementationItemCheckListItems.Add(new ImplementationItemCheckListItem
            {
                ImplementationItemId = id,
                CheckListItemId = checkListId
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Dodato {newCheckListIds.Count} stavki čekliste." });
    }

    /// <summary>
    /// Remove check list item from implementation item
    /// </summary>
    [HttpDelete("{id}/checklists/{linkId}")]
    public async Task<IActionResult> RemoveCheckListItem(int id, int linkId)
    {
        var link = await _context.ImplementationItemCheckListItems
            .Where(ic => ic.Id == linkId && ic.ImplementationItemId == id)
            .FirstOrDefaultAsync();

        if (link == null)
        {
            return NotFound(new { message = "Stavka čekliste nije pronađena." });
        }

        _context.ImplementationItemCheckListItems.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

/// <summary>
/// DTO for implementation item with check lists
/// </summary>
public class ImplementationItemDto
{
    public int Id { get; set; }
    public int ImplementationModelId { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string? Detalji { get; set; }
    public List<CheckListItemDto> CheckListItems { get; set; } = new();
}

/// <summary>
/// DTO for check list item
/// </summary>
public class CheckListItemDto
{
    public int Id { get; set; }
    public string Opis { get; set; } = string.Empty;
    public decimal? Kompleksnost { get; set; }
    public int? LinkId { get; set; }  // ID in junction table for deletion
}

/// <summary>
/// DTO for adding multiple check list items
/// </summary>
public class AddCheckListItemsDto
{
    public List<int> CheckListItemIds { get; set; } = new();
}

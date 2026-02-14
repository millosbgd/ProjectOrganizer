using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectImplementationItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProjectImplementationItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ProjectImplementationItems/project/5
    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetByProjectId(int projectId)
    {
        var items = await _context.ProjectImplementationItems
            .Include(pi => pi.ImplementationItem)
            .Where(pi => pi.ProjectId == projectId)
            .OrderBy(pi => pi.Id)
            .Select(pi => new
            {
                pi.Id,
                pi.ProjectId,
                pi.ImplementationModelId,
                pi.ImplementationItemId,
                ImplementationItemNaziv = pi.ImplementationItem != null ? pi.ImplementationItem.Naziv : null,
                pi.Napomena,
                pi.Zavrseno,
                pi.ZavrsenoDatum,
                pi.KlijentPotvrdio,
                pi.KlijentPotvrdioDatum
            })
            .ToListAsync();

        return Ok(items);
    }

    // PUT: api/ProjectImplementationItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProjectImplementationItem item)
    {
        if (id != item.Id)
            return BadRequest();

        var existingItem = await _context.ProjectImplementationItems.FindAsync(id);
        if (existingItem == null)
            return NotFound();

        existingItem.Napomena = item.Napomena;
        existingItem.Zavrseno = item.Zavrseno;
        existingItem.ZavrsenoDatum = item.ZavrsenoDatum;
        existingItem.KlijentPotvrdio = item.KlijentPotvrdio;
        existingItem.KlijentPotvrdioDatum = item.KlijentPotvrdioDatum;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.ProjectImplementationItems.AnyAsync(e => e.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }
}

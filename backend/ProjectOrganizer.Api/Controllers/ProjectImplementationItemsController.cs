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
            .Include(pi => pi.CheckLists)
                .ThenInclude(cl => cl.CheckListItem)
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
                pi.KlijentPotvrdioDatum,
                CheckLists = pi.CheckLists.Select(cl => new
                {
                    cl.Id,
                    cl.CheckListItemId,
                    CheckListItemOpis = cl.CheckListItem != null ? cl.CheckListItem.Opis : null,
                    CheckListItemKompleksnost = cl.CheckListItem != null ? cl.CheckListItem.Kompleksnost : null,
                    cl.Procenat,
                    cl.Zavrsen,
                    cl.ZavrsenDatum,
                    cl.KlijentPotvrdio,
                    cl.KlijentPotvrdioDatum
                }).ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/ProjectImplementationItems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var item = await _context.ProjectImplementationItems
            .Include(pi => pi.ImplementationItem)
            .Include(pi => pi.CheckLists)
                .ThenInclude(cl => cl.CheckListItem)
            .Where(pi => pi.Id == id)
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
                pi.KlijentPotvrdioDatum,
                CheckLists = pi.CheckLists.Select(cl => new
                {
                    cl.Id,
                    cl.CheckListItemId,
                    CheckListItemOpis = cl.CheckListItem != null ? cl.CheckListItem.Opis : null,
                    CheckListItemKompleksnost = cl.CheckListItem != null ? cl.CheckListItem.Kompleksnost : null,
                    cl.Procenat,
                    cl.Zavrsen,
                    cl.ZavrsenDatum,
                    cl.KlijentPotvrdio,
                    cl.KlijentPotvrdioDatum
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    // PUT: api/ProjectImplementationItems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProjectImplementationItem item)
    {
        if (id != item.Id)
            return BadRequest();

        var existingItem = await _context.ProjectImplementationItems
            .Include(pi => pi.CheckLists)
            .FirstOrDefaultAsync(pi => pi.Id == id);

        if (existingItem == null)
            return NotFound();

        // Validate: Cannot mark as Zavrseno or KlijentPotvrdio if not all checklists are completed
        if ((item.Zavrseno || item.KlijentPotvrdio) && existingItem.CheckLists.Any())
        {
            var allCheckListsCompleted = existingItem.CheckLists.All(cl => cl.Zavrsen);
            if (!allCheckListsCompleted)
            {
                return BadRequest(new 
                { 
                    message = "Sve stavke čekliste moraju biti završene pre nego što označite stavku kao završenu ili potvrđenu." 
                });
            }
        }

        existingItem.Napomena = item.Napomena;
        existingItem.Zavrseno = item.Zavrseno;
        existingItem.ZavrsenoDatum = item.Zavrseno ? (item.ZavrsenoDatum ?? DateOnly.FromDateTime(DateTime.UtcNow)) : null;
        existingItem.KlijentPotvrdio = item.KlijentPotvrdio;
        existingItem.KlijentPotvrdioDatum = item.KlijentPotvrdio ? (item.KlijentPotvrdioDatum ?? DateOnly.FromDateTime(DateTime.UtcNow)) : null;

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

    // PUT: api/ProjectImplementationItems/5/checklist/3
    [HttpPut("{id}/checklist/{checklistId}")]
    public async Task<IActionResult> UpdateCheckList(int id, int checklistId, [FromBody] UpdateCheckListDto dto)
    {
        var checkList = await _context.ProjectImplementationItemCheckLists
            .FirstOrDefaultAsync(cl => cl.Id == checklistId && cl.ProjectImplementationItemId == id);

        if (checkList == null)
            return NotFound(new { message = "Stavka čekliste nije pronađena." });

        if (dto.Zavrsen.HasValue)
        {
            checkList.Zavrsen = dto.Zavrsen.Value;
            checkList.ZavrsenDatum = dto.Zavrsen.Value ? (dto.ZavrsenDatum ?? DateOnly.FromDateTime(DateTime.UtcNow)) : null;
        }

        if (dto.KlijentPotvrdio.HasValue)
        {
            checkList.KlijentPotvrdio = dto.KlijentPotvrdio.Value;
            checkList.KlijentPotvrdioDatum = dto.KlijentPotvrdio.Value ? (dto.KlijentPotvrdioDatum ?? DateOnly.FromDateTime(DateTime.UtcNow)) : null;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class UpdateCheckListDto
{
    public bool? Zavrsen { get; set; }
    public DateOnly? ZavrsenDatum { get; set; }
    public bool? KlijentPotvrdio { get; set; }
    public DateOnly? KlijentPotvrdioDatum { get; set; }
}

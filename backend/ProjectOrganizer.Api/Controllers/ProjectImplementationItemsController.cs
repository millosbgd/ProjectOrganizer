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
                    CheckListItemOpis = cl.CheckListItemId == -1
                        ? cl.Opis
                        : (cl.CheckListItem != null ? cl.CheckListItem.Opis : null),
                    CheckListItemKompleksnost = cl.CheckListItem != null ? cl.CheckListItem.Kompleksnost : null,
                    cl.Procenat,
                    cl.DetaljanOpis,
                    cl.PlaniraniRok,
                    cl.Zavrsen,
                    cl.ZavrsenDatum,
                    cl.KlijentPotvrdio,
                    cl.KlijentPotvrdioDatum
                }).OrderBy(cl => cl.Id).ToList()
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
                    CheckListItemOpis = cl.CheckListItemId == -1
                        ? cl.Opis
                        : (cl.CheckListItem != null ? cl.CheckListItem.Opis : null),
                    CheckListItemKompleksnost = cl.CheckListItem != null ? cl.CheckListItem.Kompleksnost : null,
                    cl.Procenat,
                    cl.DetaljanOpis,
                    cl.PlaniraniRok,
                    cl.Zavrsen,
                    cl.ZavrsenDatum,
                    cl.KlijentPotvrdio,
                    cl.KlijentPotvrdioDatum
                }).OrderBy(cl => cl.Id).ToList()
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

        if (dto.DetaljanOpis != null)
            checkList.DetaljanOpis = string.IsNullOrWhiteSpace(dto.DetaljanOpis) ? null : dto.DetaljanOpis.Trim();

        if (dto.PlaniraniRok.HasValue || dto.ClearPlaniraniRok == true)
            checkList.PlaniraniRok = dto.ClearPlaniraniRok == true ? null : dto.PlaniraniRok;

        bool recalcNeeded = false;
        if (checkList.CheckListItemId == -1 && dto.Kompleksnost.HasValue)
        {
            checkList.Kompleksnost = dto.Kompleksnost.Value > 0 ? dto.Kompleksnost : null;
            recalcNeeded = true;
        }

        if (checkList.CheckListItemId == -1 && dto.Opis != null)
            checkList.Opis = string.IsNullOrWhiteSpace(dto.Opis) ? checkList.Opis : dto.Opis.Trim();

        await _context.SaveChangesAsync();

        if (recalcNeeded)
            await RecalculateProcentAsync(id);

        return NoContent();
    }

    // POST: api/ProjectImplementationItems/5/checklist
    [HttpPost("{id}/checklist")]
    public async Task<ActionResult<object>> AddCheckListItem(int id, [FromBody] CreateCheckListItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Opis))
            return BadRequest(new { message = "Opis je obavezan." });

        var parentItem = await _context.ProjectImplementationItems.FindAsync(id);
        if (parentItem == null)
            return NotFound();

        var newItem = new ProjectImplementationItemCheckList
        {
            ProjectImplementationItemId = id,
            CheckListItemId = -1,
            Opis = dto.Opis.Trim(),
            DetaljanOpis = string.IsNullOrWhiteSpace(dto.DetaljanOpis) ? null : dto.DetaljanOpis.Trim(),
            PlaniraniRok = dto.PlaniraniRok,
            Kompleksnost = dto.Kompleksnost,
            Procenat = null // will be calculated below
        };

        _context.ProjectImplementationItemCheckLists.Add(newItem);
        await _context.SaveChangesAsync();

        await RecalculateProcentAsync(id);

        // Reload to return updated Procenat
        await _context.Entry(newItem).ReloadAsync();

        return Ok(new
        {
            newItem.Id,
            newItem.CheckListItemId,
            CheckListItemOpis = newItem.Opis,
            CheckListItemKompleksnost = newItem.Kompleksnost,
            newItem.Procenat,
            newItem.DetaljanOpis,
            newItem.PlaniraniRok,
            Zavrsen = false,
            ZavrsenDatum = (DateOnly?)null,
            KlijentPotvrdio = false,
            KlijentPotvrdioDatum = (DateOnly?)null
        });
    }

    // DELETE: api/ProjectImplementationItems/5/checklist/3
    [HttpDelete("{id}/checklist/{checklistId}")]
    public async Task<IActionResult> DeleteCheckListItem(int id, int checklistId)
    {
        var checkList = await _context.ProjectImplementationItemCheckLists
            .FirstOrDefaultAsync(cl => cl.Id == checklistId && cl.ProjectImplementationItemId == id && cl.CheckListItemId == -1);

        if (checkList == null)
            return NotFound(new { message = "Stavka nije pronađena ili nije prilagođena stavka." });

        _context.ProjectImplementationItemCheckLists.Remove(checkList);
        await _context.SaveChangesAsync();

        await RecalculateProcentAsync(id);

        return NoContent();
    }

    private async Task RecalculateProcentAsync(int projectImplementationItemId)
    {
        // Find the parent item to determine the scope (project + implementation model)
        var parentItem = await _context.ProjectImplementationItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == projectImplementationItemId);

        if (parentItem == null) return;

        // Get all implementation item IDs in this plan (same project + same model)
        var planItemIds = await _context.ProjectImplementationItems
            .Where(i => i.ProjectId == parentItem.ProjectId
                     && i.ImplementationModelId == parentItem.ImplementationModelId)
            .Select(i => i.Id)
            .ToListAsync();

        // Get ALL checklist items across the entire plan
        var allItems = await _context.ProjectImplementationItemCheckLists
            .Include(cl => cl.CheckListItem)
            .Where(cl => planItemIds.Contains(cl.ProjectImplementationItemId))
            .ToListAsync();

        decimal totalKompleksnost = allItems.Sum(cl =>
            cl.CheckListItemId == -1
                ? (cl.Kompleksnost ?? 0)
                : (cl.CheckListItem?.Kompleksnost ?? 0));

        foreach (var cl in allItems)
        {
            decimal effectiveK = cl.CheckListItemId == -1
                ? (cl.Kompleksnost ?? 0)
                : (cl.CheckListItem?.Kompleksnost ?? 0);

            cl.Procenat = (totalKompleksnost > 0 && effectiveK > 0)
                ? Math.Round((effectiveK / totalKompleksnost) * 100, 2)
                : (decimal?)null;
        }

        await _context.SaveChangesAsync();
    }
}

public class UpdateCheckListDto
{
    public bool? Zavrsen { get; set; }
    public DateOnly? ZavrsenDatum { get; set; }
    public bool? KlijentPotvrdio { get; set; }
    public DateOnly? KlijentPotvrdioDatum { get; set; }
    public string? DetaljanOpis { get; set; }
    public DateOnly? PlaniraniRok { get; set; }
    public bool? ClearPlaniraniRok { get; set; }
    public decimal? Kompleksnost { get; set; }
    public string? Opis { get; set; }
}

public class CreateCheckListItemDto
{
    public string Opis { get; set; } = string.Empty;
    public string? DetaljanOpis { get; set; }
    public DateOnly? PlaniraniRok { get; set; }
    public decimal? Kompleksnost { get; set; }
}

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
public class ProjektiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<ProjektiController> _logger;

    public ProjektiController(ApplicationDbContext context, UserService userService, ILogger<ProjektiController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    // GET: api/Projekti
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Projekat>>> GetProjekti(
        [FromQuery] bool? aktivan = null,
        [FromQuery] string? status = null,
        [FromQuery] bool createdByMe = true)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        
        var query = _context.Projekti
            .Include(p => p.Klijent)
            .Include(p => p.Aktivnosti)
            .Include(p => p.CreatedByUser)
            .AsQueryable();

        // Filter by creator if requested (default)
        if (createdByMe)
        {
            // Show only projects created by current user
            query = query.Where(p => p.CreatedBy == currentUser.Id);
        }
        else
        {
            // Show all accessible projects
            // Admins can see all projects when createdByMe=false
            // Other users see projects with permissions + their own projects
            if (currentUser.Role != "Admin")
            {
                var userProjectIds = await _context.ProjectPermissions
                    .Where(p => p.UserId == currentUser.Id)
                    .Select(p => p.ProjekatId)
                    .ToListAsync();
                
                query = query.Where(p => userProjectIds.Contains(p.Id) || p.CreatedBy == currentUser.Id);
            }
        }

        if (aktivan.HasValue)
            query = query.Where(p => p.Aktivan == aktivan.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var projekti = await query.OrderByDescending(p => p.Datum).ToListAsync();
        return Ok(projekti);
    }

    // GET: api/Projekti/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Projekat>> GetProjekat(int id)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);
        
        // Check permission
        if (currentUser.Role != "Admin")
        {
            var hasPermission = await _context.ProjectPermissions
                .AnyAsync(p => p.ProjekatId == id && p.UserId == currentUser.Id);
            
            if (!hasPermission)
            {
                return Forbid();
            }
        }
        
        var projekat = await _context.Projekti
            .Include(p => p.Klijent)
            .Include(p => p.Aktivnosti)
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (projekat == null)
            return NotFound();

        return Ok(projekat);
    }

    // POST: api/Projekti
    [HttpPost]
    public async Task<ActionResult<Projekat>> CreateProjekat(CreateProjekatDto dto)
    {
        // Check if Klijent exists
        if (!await _context.Klijenti.AnyAsync(k => k.Id == dto.KlijentId))
            return BadRequest("Klijent ne postoji.");

        var currentUser = await _userService.EnsureUserExistsAsync(User);

        // Create projekat with auto-generated BrojProjekta
        var projekat = new Projekat
        {
            BrojProjekta = await GenerateDocumentNumber("Projekat"),
            Datum = dto.Datum,
            Naziv = dto.Naziv,
            Aktivan = dto.Aktivan,
            Status = dto.Status,
            KlijentId = dto.KlijentId,
            CreatedBy = currentUser.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Projekti.Add(projekat);
        await _context.SaveChangesAsync();

        // Grant permission to creator (if not admin)
        if (currentUser.Role != "Admin")
        {
            var permission = new ProjectPermission
            {
                ProjekatId = projekat.Id,
                UserId = currentUser.Id,
                PermissionLevel = "Admin"
            };
            _context.ProjectPermissions.Add(permission);
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetProjekat), new { id = projekat.Id }, projekat);
    }

    private async Task<string> GenerateDocumentNumber(string documentType)
    {
        var currentYear = DateTime.UtcNow.Year;

        // Get or create numbering record for this year and type
        var numbering = await _context.DocumentNumbering
            .FirstOrDefaultAsync(d => d.Year == currentYear && d.DocumentType == documentType);

        if (numbering == null)
        {
            numbering = new DocumentNumbering
            {
                Year = currentYear,
                DocumentType = documentType,
                LastNumber = 0
            };
            _context.DocumentNumbering.Add(numbering);
        }

        // Increment number
        numbering.LastNumber++;
        numbering.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Format: YYYY-NNN
        return $"{currentYear}-{numbering.LastNumber:D3}";
    }

    // PUT: api/Projekti/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProjekat(int id, Projekat projekat)
    {
        if (id != projekat.Id)
            return BadRequest();

        var existingProjekat = await _context.Projekti.FindAsync(id);
        if (existingProjekat == null)
            return NotFound();

        // Check if BrojProjekta is being changed and if it conflicts
        if (existingProjekat.BrojProjekta != projekat.BrojProjekta)
        {
            if (await _context.Projekti.AnyAsync(p => p.BrojProjekta == projekat.BrojProjekta && p.Id != id))
                return BadRequest("Projekat sa ovim brojem već postoji.");
        }

        // Update properties
        existingProjekat.BrojProjekta = projekat.BrojProjekta;
        existingProjekat.Datum = projekat.Datum;
        existingProjekat.Naziv = projekat.Naziv;
        existingProjekat.Aktivan = projekat.Aktivan;
        existingProjekat.Status = projekat.Status;
        existingProjekat.KlijentId = projekat.KlijentId;
        existingProjekat.DevOpsOrganization = projekat.DevOpsOrganization;
        existingProjekat.DevOpsProject = projekat.DevOpsProject;
        existingProjekat.DevOpsAreaPath = projekat.DevOpsAreaPath;
        existingProjekat.DevOpsIterationPath = projekat.DevOpsIterationPath;
        
        // Handle ImplementationModel change
        if (existingProjekat.ImplementationModelId != projekat.ImplementationModelId)
        {
            // Remove old implementation items if model is being changed
            if (existingProjekat.ImplementationModelId.HasValue)
            {
                var oldItems = await _context.ProjectImplementationItems
                    .Where(pi => pi.ProjectId == id)
                    .ToListAsync();
                _context.ProjectImplementationItems.RemoveRange(oldItems);
            }

            existingProjekat.ImplementationModelId = projekat.ImplementationModelId;

            // Create new implementation items if model is selected
            if (projekat.ImplementationModelId.HasValue)
            {
                await CreateProjectImplementationItems(id, projekat.ImplementationModelId.Value);
            }
        }
        
        existingProjekat.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Projekti.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Projekti/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProjekat(int id)
    {
        var projekat = await _context.Projekti.FindAsync(id);
        if (projekat == null)
            return NotFound();

        _context.Projekti.Remove(projekat);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task CreateProjectImplementationItems(int projectId, int implementationModelId)
    {
        // Get all items for the selected implementation model
        var implementationItems = await _context.ImplementationItems
            .Where(i => i.ImplementationModelId == implementationModelId)
            .ToListAsync();

        // Create ProjectImplementationItem for each item in the model
        foreach (var item in implementationItems)
        {
            var projectItem = new ProjectImplementationItem
            {
                ProjectId = projectId,
                ImplementationModelId = implementationModelId,
                ImplementationItemId = item.Id,
                Zavrseno = false,
                KlijentPotvrdio = false
            };
            _context.ProjectImplementationItems.Add(projectItem);
        }

        await _context.SaveChangesAsync();
    }
}

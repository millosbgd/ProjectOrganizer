using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjektiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProjektiController> _logger;

    public ProjektiController(ApplicationDbContext context, ILogger<ProjektiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Projekti
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Projekat>>> GetProjekti(
        [FromQuery] bool? aktivan = null,
        [FromQuery] string? status = null)
    {
        var query = _context.Projekti
            .Include(p => p.Klijent)
            .Include(p => p.Aktivnosti)
            .AsQueryable();

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
        var projekat = await _context.Projekti
            .Include(p => p.Klijent)
            .Include(p => p.Aktivnosti)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (projekat == null)
            return NotFound();

        return Ok(projekat);
    }

    // POST: api/Projekti
    [HttpPost]
    public async Task<ActionResult<Projekat>> CreateProjekat(Projekat projekat)
    {
        // Check if Klijent exists
        if (!await _context.Klijenti.AnyAsync(k => k.Id == projekat.KlijentId))
            return BadRequest("Klijent ne postoji.");

        // Auto-generate BrojProjekta
        projekat.BrojProjekta = await GenerateDocumentNumber("Projekat");
        projekat.CreatedAt = DateTime.UtcNow;
        projekat.UpdatedAt = DateTime.UtcNow;

        _context.Projekti.Add(projekat);
        await _context.SaveChangesAsync();

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
}

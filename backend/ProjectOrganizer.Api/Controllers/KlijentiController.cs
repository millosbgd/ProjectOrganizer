using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KlijentiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KlijentiController> _logger;

    public KlijentiController(ApplicationDbContext context, ILogger<KlijentiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Klijenti
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Klijent>>> GetKlijenti()
    {
        var klijenti = await _context.Klijenti
            .OrderBy(k => k.Naziv)
            .ToListAsync();
        return Ok(klijenti);
    }

    // GET: api/Klijenti/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Klijent>> GetKlijent(int id)
    {
        var klijent = await _context.Klijenti
            .Include(k => k.Projekti)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (klijent == null)
            return NotFound();

        return Ok(klijent);
    }

    // POST: api/Klijenti
    [HttpPost]
    public async Task<ActionResult<Klijent>> CreateKlijent(Klijent klijent)
    {
        klijent.CreatedAt = DateTime.UtcNow;
        klijent.UpdatedAt = DateTime.UtcNow;

        _context.Klijenti.Add(klijent);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetKlijent), new { id = klijent.Id }, klijent);
    }

    // PUT: api/Klijenti/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKlijent(int id, Klijent klijent)
    {
        if (id != klijent.Id)
            return BadRequest();

        var existingKlijent = await _context.Klijenti.FindAsync(id);
        if (existingKlijent == null)
            return NotFound();

        existingKlijent.Naziv = klijent.Naziv;
        existingKlijent.Adresa = klijent.Adresa;
        existingKlijent.Grad = klijent.Grad;
        existingKlijent.Zemlja = klijent.Zemlja;
        existingKlijent.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Klijenti.AnyAsync(k => k.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Klijenti/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKlijent(int id)
    {
        var klijent = await _context.Klijenti.FindAsync(id);
        if (klijent == null)
            return NotFound();

        // Check if klijent has associated projekti
        if (await _context.Projekti.AnyAsync(p => p.KlijentId == id))
            return BadRequest("Ne možete obrisati klijenta koji ima dodeljene projekte.");

        _context.Klijenti.Remove(klijent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

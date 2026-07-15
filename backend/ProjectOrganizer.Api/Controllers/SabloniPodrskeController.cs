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
public class SabloniPodrskeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<SabloniPodrskeController> _logger;

    public SabloniPodrskeController(
        ApplicationDbContext context,
        UserService userService,
        ILogger<SabloniPodrskeController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SablonPodrske>>> GetSabloniPodrske([FromQuery] bool myTemplatesOnly = true)
    {
        var query = _context.SabloniPodrske
            .Include(s => s.Klijent)
            .Include(s => s.KreiraoUser)
            .Include(s => s.PromenioUser)
            .AsQueryable();

        if (myTemplatesOnly)
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            query = query.Where(s => s.Kreirao == currentUser.Id);
        }

        var sabloni = await query
            .OrderByDescending(s => s.VremePromene)
            .ThenByDescending(s => s.VremeKreiranja)
            .ToListAsync();

        return Ok(sabloni);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SablonPodrske>> GetSablonPodrske(int id)
    {
        var sablon = await _context.SabloniPodrske
            .Include(s => s.Klijent)
            .Include(s => s.KreiraoUser)
            .Include(s => s.PromenioUser)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sablon == null)
            return NotFound();

        return Ok(sablon);
    }

    [HttpPost]
    public async Task<ActionResult<SablonPodrske>> CreateSablonPodrske(SablonPodrske sablon)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var validationError = await ValidateSablonAsync(sablon);
            if (validationError != null)
                return validationError;

            var now = DateTime.UtcNow;
            sablon.Id = 0;
            sablon.Kreirao = currentUser.Id;
            sablon.Promenio = currentUser.Id;
            sablon.VremeKreiranja = now;
            sablon.VremePromene = now;

            _context.SabloniPodrske.Add(sablon);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSablonPodrske), new { id = sablon.Id }, sablon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating support template");
            return StatusCode(500, "Error creating support template");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSablonPodrske(int id, SablonPodrske sablon)
    {
        try
        {
            if (id != sablon.Id)
                return BadRequest();

            var existingSablon = await _context.SabloniPodrske.FindAsync(id);
            if (existingSablon == null)
                return NotFound();

            var validationError = await ValidateSablonAsync(sablon);
            if (validationError != null)
                return validationError;

            var currentUser = await _userService.EnsureUserExistsAsync(User);

            existingSablon.KlijentId = sablon.KlijentId;
            existingSablon.OpisZahteva = sablon.OpisZahteva;
            existingSablon.OpisResenja = sablon.OpisResenja;
            existingSablon.OdgovorKlijentu = sablon.OdgovorKlijentu;
            existingSablon.Promenio = currentUser.Id;
            existingSablon.VremePromene = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating support template {Id}", id);
            return StatusCode(500, "Error updating support template");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSablonPodrske(int id)
    {
        var sablon = await _context.SabloniPodrske.FindAsync(id);
        if (sablon == null)
            return NotFound();

        _context.SabloniPodrske.Remove(sablon);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ActionResult?> ValidateSablonAsync(SablonPodrske sablon)
    {
        if (sablon.KlijentId.HasValue && !await _context.Klijenti.AnyAsync(k => k.Id == sablon.KlijentId.Value))
            return BadRequest("Klijent ne postoji.");

        if (string.IsNullOrWhiteSpace(sablon.OpisZahteva))
            return BadRequest("Opis zahteva je obavezan.");

        if (string.IsNullOrWhiteSpace(sablon.OpisResenja))
            return BadRequest("Opis rešenja je obavezan.");

        if (string.IsNullOrWhiteSpace(sablon.OdgovorKlijentu))
            return BadRequest("Odgovor klijentu je obavezan.");

        sablon.OpisZahteva = sablon.OpisZahteva.Trim();
        sablon.OpisResenja = sablon.OpisResenja.Trim();
        sablon.OdgovorKlijentu = sablon.OdgovorKlijentu.Trim();

        return null;
    }
}

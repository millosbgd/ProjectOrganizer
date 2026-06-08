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
public class PoseteGorivoController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<PoseteGorivoController> _logger;

    public PoseteGorivoController(
        ApplicationDbContext context,
        UserService userService,
        ILogger<PoseteGorivoController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("visits")]
    public async Task<ActionResult<IEnumerable<ClientVisit>>> GetVisits()
    {
        var visits = await _context.ClientVisits
            .Include(v => v.Klijent)
            .Include(v => v.Aktivnost)
            .Include(v => v.CreatedByUser)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        return Ok(visits);
    }

    [HttpGet("visits/{id}")]
    public async Task<ActionResult<ClientVisit>> GetVisit(int id)
    {
        var visit = await _context.ClientVisits
            .Include(v => v.Klijent)
            .Include(v => v.Aktivnost)
            .Include(v => v.CreatedByUser)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (visit == null)
            return NotFound();

        return Ok(visit);
    }

    [HttpPost("visits")]
    public async Task<ActionResult<ClientVisit>> CreateVisit(ClientVisit visit)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            if (!await _context.Klijenti.AnyAsync(k => k.Id == visit.KlijentId))
                return BadRequest("Klijent ne postoji.");

            if (!await _context.Aktivnosti.AnyAsync(a => a.Id == visit.AktivnostId))
                return BadRequest("Aktivnost ne postoji.");

            visit.Id = 0;
            visit.CreatedBy = currentUser.Id;
            visit.CreatedAt = DateTime.UtcNow;
            visit.UpdatedAt = DateTime.UtcNow;

            _context.ClientVisits.Add(visit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVisit), new { id = visit.Id }, visit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client visit");
            return StatusCode(500, "Error creating client visit");
        }
    }

    [HttpPut("visits/{id}")]
    public async Task<IActionResult> UpdateVisit(int id, ClientVisit visit)
    {
        try
        {
            if (id != visit.Id)
                return BadRequest();

            var existingVisit = await _context.ClientVisits.FindAsync(id);
            if (existingVisit == null)
                return NotFound();

            if (!await _context.Klijenti.AnyAsync(k => k.Id == visit.KlijentId))
                return BadRequest("Klijent ne postoji.");

            if (!await _context.Aktivnosti.AnyAsync(a => a.Id == visit.AktivnostId))
                return BadRequest("Aktivnost ne postoji.");

            existingVisit.KlijentId = visit.KlijentId;
            existingVisit.AktivnostId = visit.AktivnostId;
            existingVisit.Grad = visit.Grad;
            existingVisit.Kilometraza = visit.Kilometraza;
            existingVisit.GorivoLitara = visit.GorivoLitara;
            existingVisit.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client visit {Id}", id);
            return StatusCode(500, "Error updating client visit");
        }
    }

    [HttpDelete("visits/{id}")]
    public async Task<IActionResult> DeleteVisit(int id)
    {
        var visit = await _context.ClientVisits.FindAsync(id);
        if (visit == null)
            return NotFound();

        _context.ClientVisits.Remove(visit);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("fuel-purchases")]
    public async Task<ActionResult<IEnumerable<FuelPurchase>>> GetFuelPurchases()
    {
        var fuelPurchases = await _context.FuelPurchases
            .Include(p => p.CreatedByUser)
            .OrderByDescending(p => p.Datum)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(fuelPurchases);
    }

    [HttpGet("fuel-purchases/{id}")]
    public async Task<ActionResult<FuelPurchase>> GetFuelPurchase(int id)
    {
        var fuelPurchase = await _context.FuelPurchases
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (fuelPurchase == null)
            return NotFound();

        return Ok(fuelPurchase);
    }

    [HttpPost("fuel-purchases")]
    public async Task<ActionResult<FuelPurchase>> CreateFuelPurchase(FuelPurchase fuelPurchase)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            fuelPurchase.Id = 0;
            fuelPurchase.UkupnaCena = CalculateTotal(fuelPurchase.Kolicina, fuelPurchase.JedinicnaCena);
            fuelPurchase.CreatedBy = currentUser.Id;
            fuelPurchase.CreatedAt = DateTime.UtcNow;
            fuelPurchase.UpdatedAt = DateTime.UtcNow;

            _context.FuelPurchases.Add(fuelPurchase);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFuelPurchase), new { id = fuelPurchase.Id }, fuelPurchase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fuel purchase");
            return StatusCode(500, "Error creating fuel purchase");
        }
    }

    [HttpPut("fuel-purchases/{id}")]
    public async Task<IActionResult> UpdateFuelPurchase(int id, FuelPurchase fuelPurchase)
    {
        try
        {
            if (id != fuelPurchase.Id)
                return BadRequest();

            var existingFuelPurchase = await _context.FuelPurchases.FindAsync(id);
            if (existingFuelPurchase == null)
                return NotFound();

            existingFuelPurchase.Datum = fuelPurchase.Datum;
            existingFuelPurchase.Kolicina = fuelPurchase.Kolicina;
            existingFuelPurchase.JedinicnaCena = fuelPurchase.JedinicnaCena;
            existingFuelPurchase.UkupnaCena = CalculateTotal(fuelPurchase.Kolicina, fuelPurchase.JedinicnaCena);
            existingFuelPurchase.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fuel purchase {Id}", id);
            return StatusCode(500, "Error updating fuel purchase");
        }
    }

    [HttpDelete("fuel-purchases/{id}")]
    public async Task<IActionResult> DeleteFuelPurchase(int id)
    {
        var fuelPurchase = await _context.FuelPurchases.FindAsync(id);
        if (fuelPurchase == null)
            return NotFound();

        _context.FuelPurchases.Remove(fuelPurchase);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static decimal CalculateTotal(decimal quantity, decimal unitPrice)
    {
        return Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Services;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoogleSheetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly GoogleSheetsService _googleSheetsService;

    public GoogleSheetsController(ApplicationDbContext context, GoogleSheetsService googleSheetsService)
    {
        _context = context;
        _googleSheetsService = googleSheetsService;
    }

    // POST: api/GoogleSheets/projekat/5/generate
    [HttpPost("projekat/{projekatId}/generate")]
    public async Task<IActionResult> GenerateSheet(int projekatId)
    {
        var projekat = await _context.Projekti.FindAsync(projekatId);
        if (projekat == null)
            return NotFound("Projekat nije pronađen.");

        var items = await _context.ProjectImplementationItems
            .Include(pi => pi.ImplementationItem)
            .Where(pi => pi.ProjectId == projekatId)
            .OrderBy(pi => pi.Id)
            .ToListAsync();

        if (!items.Any())
            return BadRequest("Projekat nema stavke implementacije.");

        var existingSheetId = projekat.GoogleSheetId;

        var spreadsheetId = await _googleSheetsService.CreateOrUpdateSheetAsync(
            projekat, items, existingSheetId);

        if (string.IsNullOrEmpty(existingSheetId))
        {
            projekat.GoogleSheetId = spreadsheetId;
            await _context.SaveChangesAsync();
        }

        var url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
        return Ok(new { spreadsheetId, url });
    }

    // POST: api/GoogleSheets/projekat/5/sync
    [HttpPost("projekat/{projekatId}/sync")]
    public async Task<IActionResult> SyncFromSheet(int projekatId)
    {
        var projekat = await _context.Projekti.FindAsync(projekatId);
        if (projekat == null)
            return NotFound("Projekat nije pronađen.");

        if (string.IsNullOrEmpty(projekat.GoogleSheetId))
            return BadRequest("Projekat nema generisan Sheet. Najpre generiši Sheet.");

        var items = await _context.ProjectImplementationItems
            .Where(pi => pi.ProjectId == projekatId)
            .OrderBy(pi => pi.Id)
            .ToListAsync();

        var sheetData = await _googleSheetsService.ReadSheetDataAsync(projekat.GoogleSheetId);

        int synced = 0;
        foreach (var (itemIndex, datum, potvrdeno) in sheetData)
        {
            if (itemIndex >= items.Count) break;

            var item = items[itemIndex];
            item.KlijentPotvrdio = potvrdeno;

            if (potvrdeno && !item.KlijentPotvrdioDatum.HasValue)
                item.KlijentPotvrdioDatum = DateTime.UtcNow;
            else if (!potvrdeno)
                item.KlijentPotvrdioDatum = null;

            synced++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { synced });
    }
}

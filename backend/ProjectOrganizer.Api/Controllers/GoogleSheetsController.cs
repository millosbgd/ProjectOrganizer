using Google;
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
    private readonly ILogger<GoogleSheetsController> _logger;

    public GoogleSheetsController(ApplicationDbContext context, GoogleSheetsService googleSheetsService, ILogger<GoogleSheetsController> logger)
    {
        _context = context;
        _googleSheetsService = googleSheetsService;
        _logger = logger;
    }

    // POST: api/GoogleSheets/projekat/5/generate
    [HttpPost("projekat/{projekatId}/generate")]
    public async Task<IActionResult> GenerateSheet(int projekatId)
    {
        try
        {
            var projekat = await _context.Projekti.FindAsync(projekatId);
            if (projekat == null)
                return NotFound("Projekat nije pronađen.");

            var items = await _context.ProjectImplementationItems
                .Include(pi => pi.ImplementationItem)
                .Include(pi => pi.CheckLists)
                    .ThenInclude(cl => cl.CheckListItem)
                .Where(pi => pi.ProjectId == projekatId)
                .OrderBy(pi => pi.Id)
                .ToListAsync();

            if (!items.Any())
                return BadRequest("Projekat nema stavke implementacije.");

            var existingSheetId = projekat.GoogleSheetId;

            var (spreadsheetId, _) = await _googleSheetsService.CreateOrUpdateSheetAsync(
                projekat, items, existingSheetId);

            if (string.IsNullOrEmpty(existingSheetId))
            {
                projekat.GoogleSheetId = spreadsheetId;
                await _context.SaveChangesAsync();
            }

            var url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
            return Ok(new { spreadsheetId, url });
        }
        catch (GoogleApiException gex)
        {
            var reasons = gex.Error?.Errors?.Select(e => new { e.Domain, e.Message, e.Reason }).ToList();
            _logger.LogError("GenerateSheet GoogleApiException: Status={Status} Message={Message} Reasons={Reasons}",
                gex.HttpStatusCode, gex.Message,
                string.Join(" | ", gex.Error?.Errors?.Select(e => $"{e.Domain}/{e.Reason}: {e.Message}") ?? Array.Empty<string>()));
            return StatusCode(500, new { error = "GoogleApiException", httpStatus = gex.HttpStatusCode.ToString(), message = gex.Message, reasons });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateSheet unexpected error: {Type} {Message}", ex.GetType().Name, ex.Message);
            return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // POST: api/GoogleSheets/projekat/5/sync
    [HttpPost("projekat/{projekatId}/sync")]    public async Task<IActionResult> SyncFromSheet(int projekatId)
    {
        var projekat = await _context.Projekti.FindAsync(projekatId);
        if (projekat == null)
            return NotFound("Projekat nije pronađen.");

        if (string.IsNullOrEmpty(projekat.GoogleSheetId))
            return BadRequest("Projekat nema generisan Sheet. Najpre generiši Sheet.");

        var checkLists = await _context.ProjectImplementationItems
            .Where(pi => pi.ProjectId == projekatId)
            .SelectMany(pi => pi.CheckLists)
            .ToListAsync();

        var sheetData = await _googleSheetsService.ReadSheetDataAsync(projekat.GoogleSheetId);

        int synced = 0;
        foreach (var (checkListItemId, potvrdeno, datumIzSheeta) in sheetData)
        {
            var cl = checkLists.FirstOrDefault(c => c.Id == checkListItemId);
            if (cl == null) continue;

            cl.KlijentPotvrdio = potvrdeno;
            cl.KlijentPotvrdioDatum = potvrdeno
                ? (datumIzSheeta ?? cl.KlijentPotvrdioDatum ?? DateOnly.FromDateTime(DateTime.UtcNow))
                : null;
            synced++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { synced });
    }

    // POST: api/GoogleSheets/projekat/5/generate-internal
    [HttpPost("projekat/{projekatId}/generate-internal")]
    public async Task<IActionResult> GenerateInternalSheet(int projekatId)
    {
        try
        {
            var projekat = await _context.Projekti.FindAsync(projekatId);
            if (projekat == null)
                return NotFound("Projekat nije pronađen.");

            var items = await _context.ProjectImplementationItems
                .Include(pi => pi.ImplementationItem)
                .Include(pi => pi.CheckLists)
                    .ThenInclude(cl => cl.CheckListItem)
                .Where(pi => pi.ProjectId == projekatId)
                .OrderBy(pi => pi.Id)
                .ToListAsync();

            if (!items.Any())
                return BadRequest("Projekat nema stavke implementacije.");

            var existingSheetId = projekat.InternalGoogleSheetId;

            var spreadsheetId = await _googleSheetsService.CreateOrUpdateInternalSheetAsync(
                projekat, items, existingSheetId);

            if (string.IsNullOrEmpty(existingSheetId))
            {
                projekat.InternalGoogleSheetId = spreadsheetId;
                await _context.SaveChangesAsync();
            }

            var url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}";
            return Ok(new { spreadsheetId, url });
        }
        catch (GoogleApiException gex)
        {
            var reasons = gex.Error?.Errors?.Select(e => new { e.Domain, e.Message, e.Reason }).ToList();
            _logger.LogError("GenerateInternalSheet GoogleApiException: {Status} {Message}", gex.HttpStatusCode, gex.Message);
            return StatusCode(500, new { error = "GoogleApiException", httpStatus = gex.HttpStatusCode.ToString(), message = gex.Message, reasons });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GenerateInternalSheet error: {Type} {Message}", ex.GetType().Name, ex.Message);
            return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message });
        }
    }
}

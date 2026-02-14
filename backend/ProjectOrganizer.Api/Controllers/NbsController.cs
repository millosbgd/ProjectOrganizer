using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectOrganizer.Api.Services;

namespace ProjectOrganizer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NbsController : ControllerBase
{
    private readonly NbsService _nbsService;
    private readonly ILogger<NbsController> _logger;

    public NbsController(NbsService nbsService, ILogger<NbsController> logger)
    {
        _nbsService = nbsService;
        _logger = logger;
    }

    [HttpGet("company/{pib}")]
    public async Task<ActionResult<CompanyInfo>> GetCompanyInfo(string pib)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pib))
                return BadRequest(new { message = "PIB je obavezan." });

            var companyInfo = await _nbsService.GetCompanyInformationAsync(pib);

            if (companyInfo == null)
                return NotFound(new { message = $"Kompanija sa PIB-om {pib} nije pronađena." });

            return Ok(companyInfo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "NBS operacija nije uspela za PIB {PIB}", pib);
            return StatusCode(503, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Greška pri preuzimanju podataka o kompaniji sa PIB-om {PIB}", pib);
            return StatusCode(500, new { message = "Greška pri preuzimanju podataka sa NBS servisa." });
        }
    }
}

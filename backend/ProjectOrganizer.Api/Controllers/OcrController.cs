using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Services;
using System.Security.Claims;

namespace ProjectOrganizer.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OcrController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly OpenAIService _openAIService;
    private readonly ILogger<OcrController> _logger;

    public OcrController(ApplicationDbContext context, OpenAIService openAIService, ILogger<OcrController> logger)
    {
        _context = context;
        _openAIService = openAIService;
        _logger = logger;
    }

    [HttpPost("extract-text")]
    public async Task<ActionResult<ExtractTextResponse>> ExtractText([FromBody] ExtractTextRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId);

            if (userSettings == null)
                return BadRequest("User settings not found. Please configure your OpenAI settings.");

            if (string.IsNullOrWhiteSpace(userSettings.OpenAiApiKey))
                return BadRequest("OpenAI API key not configured. Please update your settings.");

            _logger.LogInformation("Extracting text from image for user {UserId} using model {Model}", 
                userId, userSettings.OpenAiModel);

            var extractedText = await _openAIService.ExtractTextFromImageAsync(
                userSettings.OpenAiApiKey,
                userSettings.OpenAiModel,
                request.ImageBase64
            );

            return Ok(new ExtractTextResponse { Text = extractedText });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from image");
            return StatusCode(500, "Greška prilikom očitavanja teksta sa slike.");
        }
    }
}

public class ExtractTextRequest
{
    public string ImageBase64 { get; set; } = string.Empty;
}

public class ExtractTextResponse
{
    public string Text { get; set; } = string.Empty;
}

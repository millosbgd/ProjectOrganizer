using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using System.Security.Claims;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(ApplicationDbContext context, ILogger<UserSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/UserSettings
    [HttpGet]
    public async Task<ActionResult<UserSettings>> GetUserSettings()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings for new user
            settings = new UserSettings
            {
                UserId = userId,
                OpenAiModel = "gpt-4o-mini"
            };
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(settings);
    }

    // PUT: api/UserSettings
    [HttpPut]
    public async Task<IActionResult> UpdateUserSettings(UserSettings userSettings)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var existingSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existingSettings == null)
        {
            // Create new settings
            userSettings.UserId = userId;
            userSettings.CreatedAt = DateTime.UtcNow;
            userSettings.UpdatedAt = DateTime.UtcNow;
            _context.UserSettings.Add(userSettings);
        }
        else
        {
            // Update existing settings
            existingSettings.OpenAiApiKey = userSettings.OpenAiApiKey;
            existingSettings.OpenAiModel = userSettings.OpenAiModel;
            existingSettings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

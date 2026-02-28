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
public class UserSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryptionService;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(
        ApplicationDbContext context, 
        EncryptionService encryptionService,
        ILogger<UserSettingsController> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    // GET: api/UserSettings
    [HttpGet]
    public async Task<ActionResult<UserSettingsResponseDto>> GetUserSettings()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthorized access attempt to GetUserSettings");
            return Unauthorized();
        }

        var settings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings for new user
            settings = new UserSettings
            {
                UserId = userId,
                OpenAiModel = "gpt-4o-mini",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created default settings for user {userId}");
        }

        // Decrypt keys for masking (but never return them in full)
        var decryptedOpenAiKey = _encryptionService.Decrypt(settings.OpenAiApiKey);
        var decryptedDevOpsPat = _encryptionService.Decrypt(settings.DevOpsPersonalAccessToken);

        // Return DTO with masked keys
        var responseDto = new UserSettingsResponseDto
        {
            Id = settings.Id,
            UserId = settings.UserId,
            OpenAiApiKeyMasked = _encryptionService.MaskOpenAiKey(decryptedOpenAiKey),
            HasOpenAiApiKey = !string.IsNullOrEmpty(decryptedOpenAiKey),
            OpenAiModel = settings.OpenAiModel,
            DevOpsPatMasked = _encryptionService.MaskValue(decryptedDevOpsPat, 4),
            HasDevOpsPat = !string.IsNullOrEmpty(decryptedDevOpsPat),
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };

        return Ok(responseDto);
    }

    // PUT: api/UserSettings
    [HttpPut]
    public async Task<IActionResult> UpdateUserSettings(UpdateUserSettingsDto updateDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unauthorized access attempt to UpdateUserSettings");
            return Unauthorized();
        }

        // Validate model
        var allowedModels = new[] { "gpt-4o-mini", "gpt-4o", "gpt-4-turbo" };
        if (!allowedModels.Contains(updateDto.OpenAiModel))
        {
            return BadRequest("Invalid OpenAI model selected");
        }

        var existingSettings = await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existingSettings == null)
        {
            // Create new settings
            var newSettings = new UserSettings
            {
                UserId = userId,
                OpenAiModel = updateDto.OpenAiModel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Encrypt and set OpenAI API key if provided
            if (!string.IsNullOrEmpty(updateDto.OpenAiApiKey) && updateDto.OpenAiApiKey != "REMOVE")
            {
                // Basic validation for OpenAI key format
                if (!updateDto.OpenAiApiKey.StartsWith("sk-"))
                {
                    return BadRequest("Invalid OpenAI API key format. Key should start with 'sk-'");
                }
                newSettings.OpenAiApiKey = _encryptionService.Encrypt(updateDto.OpenAiApiKey);
            }

            // Encrypt and set DevOps PAT if provided
            if (!string.IsNullOrEmpty(updateDto.DevOpsPersonalAccessToken) && updateDto.DevOpsPersonalAccessToken != "REMOVE")
            {
                newSettings.DevOpsPersonalAccessToken = _encryptionService.Encrypt(updateDto.DevOpsPersonalAccessToken);
            }

            _context.UserSettings.Add(newSettings);
            _logger.LogInformation($"Created new settings for user {userId}");
        }
        else
        {
            // Update existing settings
            existingSettings.OpenAiModel = updateDto.OpenAiModel;
            existingSettings.UpdatedAt = DateTime.UtcNow;

            // Update OpenAI API key if provided
            if (updateDto.OpenAiApiKey == "REMOVE")
            {
                existingSettings.OpenAiApiKey = null;
                _logger.LogInformation($"Removed OpenAI API key for user {userId}");
            }
            else if (!string.IsNullOrEmpty(updateDto.OpenAiApiKey))
            {
                // Basic validation for OpenAI key format
                if (!updateDto.OpenAiApiKey.StartsWith("sk-"))
                {
                    return BadRequest("Invalid OpenAI API key format. Key should start with 'sk-'");
                }
                existingSettings.OpenAiApiKey = _encryptionService.Encrypt(updateDto.OpenAiApiKey);
                _logger.LogInformation($"Updated OpenAI API key for user {userId}");
            }
            // If null or empty, keep existing key

            // Update DevOps PAT if provided
            if (updateDto.DevOpsPersonalAccessToken == "REMOVE")
            {
                existingSettings.DevOpsPersonalAccessToken = null;
                _logger.LogInformation($"Removed DevOps PAT for user {userId}");
            }
            else if (!string.IsNullOrEmpty(updateDto.DevOpsPersonalAccessToken))
            {
                existingSettings.DevOpsPersonalAccessToken = _encryptionService.Encrypt(updateDto.DevOpsPersonalAccessToken);
                _logger.LogInformation($"Updated DevOps PAT for user {userId}");
            }
            // If null or empty, keep existing token
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

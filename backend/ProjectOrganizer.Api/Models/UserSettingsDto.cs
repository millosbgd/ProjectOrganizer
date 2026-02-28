using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

/// <summary>
/// DTO for reading user settings - never exposes full API keys
/// </summary>
public class UserSettingsResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Masked OpenAI API key (e.g., "sk-...****abcd") or null if not set
    /// </summary>
    public string? OpenAiApiKeyMasked { get; set; }
    
    /// <summary>
    /// Indicates whether OpenAI API key is configured
    /// </summary>
    public bool HasOpenAiApiKey { get; set; }
    
    [MaxLength(50)]
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    
    /// <summary>
    /// Masked DevOps PAT (e.g., "****1234") or null if not set
    /// </summary>
    public string? DevOpsPatMasked { get; set; }
    
    /// <summary>
    /// Indicates whether DevOps PAT is configured
    /// </summary>
    public bool HasDevOpsPat { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for updating user settings
/// </summary>
public class UpdateUserSettingsDto
{
    /// <summary>
    /// OpenAI API key - only sent when user wants to update it
    /// If null or empty, existing key is kept unchanged
    /// Use special value "REMOVE" to delete the key
    /// </summary>
    [MaxLength(500)]
    public string? OpenAiApiKey { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    
    /// <summary>
    /// DevOps Personal Access Token - only sent when user wants to update it
    /// If null or empty, existing token is kept unchanged
    /// Use special value "REMOVE" to delete the token
    /// </summary>
    [MaxLength(500)]
    public string? DevOpsPersonalAccessToken { get; set; }
}

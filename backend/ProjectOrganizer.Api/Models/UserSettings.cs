using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class UserSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? OpenAiApiKey { get; set; }

    [MaxLength(50)]
    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    [MaxLength(500)]
    public string? DevOpsPersonalAccessToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

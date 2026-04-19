using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class DevOpsUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string UniqueName { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Organization { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    public int? RoleId { get; set; }

    public Codebook? Role { get; set; }
}

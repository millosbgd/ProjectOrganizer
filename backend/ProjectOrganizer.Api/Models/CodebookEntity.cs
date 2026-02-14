using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class CodebookEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Codebook> Codebooks { get; set; } = new List<Codebook>();
}

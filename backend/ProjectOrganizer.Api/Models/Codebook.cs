using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class Codebook
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EntityTypeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Value { get; set; } = string.Empty;

    public int OrderIndex { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("EntityTypeId")]
    public CodebookEntity? EntityType { get; set; }
}

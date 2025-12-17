using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class DocumentNumbering
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    [Required]
    public int LastNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

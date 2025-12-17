using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class Dokument
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjekatId { get; set; }

    [Required]
    [MaxLength(500)]
    public string NazivFajla { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TipFajla { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string BlobUrl { get; set; } = string.Empty;

    [Required]
    public long Velicina { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Projekat? Projekat { get; set; }
}

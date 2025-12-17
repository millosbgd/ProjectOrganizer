using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class Note
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjekatId { get; set; }

    [Required]
    public string Opis { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Projekat? Projekat { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class Aktivnost
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Opis { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Detalji { get; set; } = string.Empty;

    [Required]
    public DateTime Datum { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Vrsta { get; set; } = string.Empty;

    [Required]
    public int ProjekatId { get; set; }

    public int? ProjectImplementationItemId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProjekatId")]
    public Projekat? Projekat { get; set; }

    [ForeignKey("ProjectImplementationItemId")]
    public ProjectImplementationItem? ProjectImplementationItem { get; set; }
}

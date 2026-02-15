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

    // Calendar support - UTC timestamps
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Vrsta { get; set; } = string.Empty;

    // BAU (Business As Usual) - not tied to a specific project
    public bool Bau { get; set; } = false;

    [Required]
    public int ProjekatId { get; set; }

    public int? ProjectImplementationItemId { get; set; }

    // User who created this activity
    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProjekatId")]
    public Projekat? Projekat { get; set; }

    [ForeignKey("ProjectImplementationItemId")]
    public ProjectImplementationItem? ProjectImplementationItem { get; set; }

    [ForeignKey("CreatedBy")]
    public User? CreatedByUser { get; set; }
}

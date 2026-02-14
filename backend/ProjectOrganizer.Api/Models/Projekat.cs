using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class Projekat
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string BrojProjekta { get; set; } = string.Empty;

    [Required]
    public DateTime Datum { get; set; }

    [Required]
    [MaxLength(300)]
    public string Naziv { get; set; } = string.Empty;

    public bool Aktivan { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    public int KlijentId { get; set; }

    public int? CreatedBy { get; set; }

    public int? ImplementationModelId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // DevOps configuration
    [MaxLength(200)]
    public string? DevOpsOrganization { get; set; }

    [MaxLength(200)]
    public string? DevOpsProject { get; set; }

    [MaxLength(500)]
    public string? DevOpsAreaPath { get; set; }

    [MaxLength(500)]
    public string? DevOpsIterationPath { get; set; }

    // Navigation properties
    [ForeignKey("KlijentId")]
    public Klijent? Klijent { get; set; }

    [ForeignKey("CreatedBy")]
    public User? CreatedByUser { get; set; }

    [ForeignKey("ImplementationModelId")]
    public ImplementationModel? ImplementationModel { get; set; }

    public ICollection<Aktivnost> Aktivnosti { get; set; } = new List<Aktivnost>();
    public ICollection<ProjectImplementationItem> ImplementationItems { get; set; } = new List<ProjectImplementationItem>();
}

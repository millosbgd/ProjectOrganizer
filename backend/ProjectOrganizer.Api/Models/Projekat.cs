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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("KlijentId")]
    public Klijent? Klijent { get; set; }

    public ICollection<Aktivnost> Aktivnosti { get; set; } = new List<Aktivnost>();
}

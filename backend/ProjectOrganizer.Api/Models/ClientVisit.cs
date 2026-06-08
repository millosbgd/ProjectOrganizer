using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class ClientVisit
{
    public int Id { get; set; }

    [Required]
    public int KlijentId { get; set; }

    [Required]
    public int AktivnostId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Grad { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Kilometraza { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal GorivoLitara { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Klijent? Klijent { get; set; }
    public Aktivnost? Aktivnost { get; set; }
    public User? CreatedByUser { get; set; }
}

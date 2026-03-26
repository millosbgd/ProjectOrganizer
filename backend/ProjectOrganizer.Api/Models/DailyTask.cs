using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class DailyTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateOnly Datum { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [MaxLength(500)]
    public string Opis { get; set; } = string.Empty;

    [Required]
    public int KorisnikKreirao { get; set; }

    public bool Solved { get; set; } = false;

    public bool Pinned { get; set; } = false;

    // Navigation property
    [ForeignKey("KorisnikKreirao")]
    public User? User { get; set; }
}

public class CreateDailyTaskDto
{
    [Required]
    [MaxLength(500)]
    public string Opis { get; set; } = string.Empty;
}

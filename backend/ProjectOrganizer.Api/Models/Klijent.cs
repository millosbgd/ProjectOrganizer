using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class Klijent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Adresa { get; set; }

    [MaxLength(100)]
    public string? Grad { get; set; }

    [MaxLength(100)]
    public string? Zemlja { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Projekat> Projekti { get; set; } = new List<Projekat>();
}

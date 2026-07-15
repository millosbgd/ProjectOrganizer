using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class SablonPodrske
{
    public int Id { get; set; }

    public int? KlijentId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Naslov { get; set; } = string.Empty;

    [MaxLength(500)]
    public string OpisZahteva { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string OpisResenja { get; set; } = string.Empty;

    [MaxLength(500)]
    public string OdgovorKlijentu { get; set; } = string.Empty;

    public int? Kreirao { get; set; }

    public int? Promenio { get; set; }

    public DateTime VremeKreiranja { get; set; } = DateTime.UtcNow;

    public DateTime VremePromene { get; set; } = DateTime.UtcNow;

    [ForeignKey("KlijentId")]
    public Klijent? Klijent { get; set; }

    [ForeignKey("Kreirao")]
    public User? KreiraoUser { get; set; }

    [ForeignKey("Promenio")]
    public User? PromenioUser { get; set; }
}

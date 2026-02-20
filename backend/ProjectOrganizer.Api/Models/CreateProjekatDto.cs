using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class CreateProjekatDto
{
    [Required]
    public DateOnly Datum { get; set; }

    [Required]
    [MaxLength(300)]
    public string Naziv { get; set; } = string.Empty;

    public bool Aktivan { get; set; } = true;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    public int KlijentId { get; set; }

    public int? ImplementationModelId { get; set; }
}

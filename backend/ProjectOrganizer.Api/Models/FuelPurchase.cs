using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class FuelPurchase
{
    public int Id { get; set; }

    public DateTime Datum { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal JedinicnaCena { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UkupnaCena { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? CreatedByUser { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models
{
    public class ImplementationModel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Naziv { get; set; }

        [MaxLength(400)]
        public string? Opis { get; set; }

        public bool Aktivan { get; set; } = false;

        // Navigation property
        public ICollection<ImplementationItem> Items { get; set; } = new List<ImplementationItem>();
    }
}

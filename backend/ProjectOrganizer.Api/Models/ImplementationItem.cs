using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models
{
    public class ImplementationItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ImplementationModelId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Naziv { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Detalji { get; set; }

        // Navigation properties
        [ForeignKey("ImplementationModelId")]
        public ImplementationModel? ImplementationModel { get; set; }

        public ICollection<ImplementationItemCheckListItem> CheckListItems { get; set; } = new List<ImplementationItemCheckListItem>();
    }
}

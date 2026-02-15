using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models
{
    /// <summary>
    /// Check list item codebook - reusable checklist items
    /// </summary>
    public class CheckListItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Opis { get; set; } = string.Empty;
    }
}

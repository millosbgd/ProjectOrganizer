using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>
        /// Complexity metric (1-10)
        /// </summary>
        [Range(1.0, 10.0, ErrorMessage = "Kompleksnost mora biti između 1 i 10")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Kompleksnost { get; set; }
    }
}

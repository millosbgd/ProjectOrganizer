using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models
{
    /// <summary>
    /// CheckList items for project implementation items
    /// Tracks completion status of each checklist item for a specific project implementation
    /// </summary>
    public class ProjectImplementationItemCheckList
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectImplementationItemId { get; set; }

        /// <summary>
        /// -1 for custom (ad-hoc) items not tied to implementation model codebook
        /// </summary>
        [Required]
        public int CheckListItemId { get; set; }

        /// <summary>
        /// Opis for custom items (when CheckListItemId == -1)
        /// </summary>
        [MaxLength(200)]
        public string? Opis { get; set; }

        /// <summary>
        /// Detailed description / notes for this checklist item on the project
        /// </summary>
        [MaxLength(1000)]
        public string? DetaljanOpis { get; set; }

        /// <summary>
        /// Planned completion deadline for this checklist item
        /// </summary>
        public DateOnly? PlaniraniRok { get; set; }

        /// <summary>
        /// Indicates if this checklist item is completed
        /// </summary>
        public bool Zavrsen { get; set; } = false;

        public DateOnly? ZavrsenDatum { get; set; }

        /// <summary>
        /// Percentage weight based on CheckListItem Kompleksnost
        /// Calculated as: (Kompleksnost / Sum of all Kompleksnost) * 100
        /// </summary>
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Procenat { get; set; }

        public bool KlijentPotvrdio { get; set; } = false;

        public DateOnly? KlijentPotvrdioDatum { get; set; }

        // Navigation properties
        [ForeignKey("ProjectImplementationItemId")]
        public ProjectImplementationItem? ProjectImplementationItem { get; set; }

        [ForeignKey("CheckListItemId")]
        public CheckListItem? CheckListItem { get; set; }
    }
}

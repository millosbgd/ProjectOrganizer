using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models
{
    public class ProjectImplementationItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ImplementationModelId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int ImplementationItemId { get; set; }

        [MaxLength(150)]
        public string? Napomena { get; set; }

        public bool Zavrseno { get; set; } = false;

        public DateOnly? ZavrsenoDatum { get; set; }

        public bool KlijentPotvrdio { get; set; } = false;

        public DateOnly? KlijentPotvrdioDatum { get; set; }

        // Navigation properties
        [ForeignKey("ImplementationModelId")]
        public ImplementationModel? ImplementationModel { get; set; }

        [ForeignKey("ProjectId")]
        public Projekat? Project { get; set; }

        [ForeignKey("ImplementationItemId")]
        public ImplementationItem? ImplementationItem { get; set; }

        public ICollection<ProjectImplementationItemCheckList> CheckLists { get; set; } = new List<ProjectImplementationItemCheckList>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models
{
    /// <summary>
    /// Junction table linking implementation items with check list items
    /// </summary>
    public class ImplementationItemCheckListItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ImplementationItemId { get; set; }

        [Required]
        public int CheckListItemId { get; set; }

        // Navigation properties
        [ForeignKey("ImplementationItemId")]
        public ImplementationItem? ImplementationItem { get; set; }

        [ForeignKey("CheckListItemId")]
        public CheckListItem? CheckListItem { get; set; }
    }
}

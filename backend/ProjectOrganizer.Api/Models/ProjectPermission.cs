using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class ProjectPermission
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int ProjekatId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string PermissionLevel { get; set; } = "Read"; // Read, Write, Admin
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int? CreatedBy { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public Projekat? Projekat { get; set; }
}

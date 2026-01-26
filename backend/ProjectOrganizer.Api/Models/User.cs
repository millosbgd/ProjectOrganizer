using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Auth0Id { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Name { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "User";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLogin { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

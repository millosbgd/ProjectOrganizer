using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class DevOpsTasksCandidate
{
    public int Id { get; set; }
    
    [Required]
    public int AktivnostId { get; set; }
    
    [Required]
    public string GeneratedContent { get; set; } = string.Empty;
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Sent, Rejected
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Aktivnost? Aktivnost { get; set; }
    public User? User { get; set; }
}

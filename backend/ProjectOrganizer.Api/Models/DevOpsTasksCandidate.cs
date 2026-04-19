using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class DevOpsTasksCandidate
{
    public int Id { get; set; }
    
    [Required]
    public int AktivnostId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? AcceptanceCriteria { get; set; }
    
    [MaxLength(50)]
    public string? Priority { get; set; }
    
    [MaxLength(50)]
    public string? Estimation { get; set; }
    
    [Required]
    public int OrderIndex { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Sent, Rejected

    public int? DevOpsWorkItemId { get; set; }

    [MaxLength(500)]
    public string? DevOpsUrl { get; set; }

    public ICollection<DevOpsTaskStatusHistory>? StatusHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Aktivnost? Aktivnost { get; set; }
    public User? User { get; set; }
}

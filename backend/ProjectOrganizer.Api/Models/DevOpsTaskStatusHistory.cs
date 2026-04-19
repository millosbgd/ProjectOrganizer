using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class DevOpsTaskStatusHistory
{
    public int Id { get; set; }

    [Required]
    public int DevOpsTaskCandidateId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AssignedTo { get; set; }

    [Required]
    public DateTime ChangedDate { get; set; }

    /// <summary>
    /// Duration in minutes the task spent in this status (null if still active).
    /// </summary>
    public int? DurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("DevOpsTaskCandidateId")]
    public DevOpsTasksCandidate? DevOpsTasksCandidate { get; set; }
}

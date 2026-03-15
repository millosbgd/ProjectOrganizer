using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectOrganizer.Api.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? ProjekatId { get; set; }

    /// <summary>
    /// Tip notifikacije: StatusChange | Blocked | Inactive | DeadlineApproaching
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public bool Dismissed { get; set; } = false;

    public int? AktivnostId { get; set; }

    /// <summary>
    /// Koristi se za sprečavanje duplikata za isti događaj.
    /// Format: "{Type}:{ProjekatId}:{yyyy-MM-dd}"
    /// </summary>
    [MaxLength(200)]
    public string? ReferenceKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("ProjekatId")]
    public Projekat? Projekat { get; set; }

    [ForeignKey("AktivnostId")]
    public Aktivnost? Aktivnost { get; set; }
}

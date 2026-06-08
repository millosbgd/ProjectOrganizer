using System.ComponentModel.DataAnnotations;

namespace ProjectOrganizer.Api.Models;

public class UserMenuPermission
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string MenuKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}

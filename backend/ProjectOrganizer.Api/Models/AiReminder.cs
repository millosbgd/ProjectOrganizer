namespace ProjectOrganizer.Api.Models;

public class AiReminder
{
    public int Id { get; set; }
    public int AktivnostId { get; set; }
    public int? ProjekatId { get; set; }
    public int UserId { get; set; }
    public DateTime RemindAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Sent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Aktivnost? Aktivnost { get; set; }
    public Projekat? Projekat { get; set; }
    public User? User { get; set; }
}

namespace ProjectOrganizer.Api.Models;

/// <summary>
/// Project completion statistics
/// Tracks completion percentage for last week and current week
/// </summary>
public class ProjectCompletionStats
{
    /// <summary>
    /// Completion percentage as of last week (7 days ago)
    /// </summary>
    public decimal LastWeekCompletion { get; set; }

    /// <summary>
    /// Current completion percentage
    /// </summary>
    public decimal CurrentCompletion { get; set; }

    /// <summary>
    /// Difference between current and last week (positive = progress, negative = regression)
    /// </summary>
    public decimal CompletionDelta => CurrentCompletion - LastWeekCompletion;

    /// <summary>
    /// Indicates if project has implementation plan defined
    /// </summary>
    public bool HasImplementationPlan { get; set; }
}

/// <summary>
/// Extended Projekat DTO with completion statistics
/// </summary>
public class ProjekatWithStatsDto
{
    public int Id { get; set; }
    public string BrojProjekta { get; set; } = string.Empty;
    public DateOnly Datum { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public bool Aktivan { get; set; }
    public string Status { get; set; } = string.Empty;
    public int KlijentId { get; set; }
    public int? CreatedBy { get; set; }
    public int? ImplementationModelId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Klijent? Klijent { get; set; }
    public User? CreatedByUser { get; set; }
    
    // Completion statistics
    public ProjectCompletionStats? CompletionStats { get; set; }
}

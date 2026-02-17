namespace ProjectOrganizer.Api.Models;

/// <summary>
/// Glavni model za Dashboard statistike
/// </summary>
public class DashboardStats
{
    public int ActiveProjectsCount { get; set; }
    public int UnfinishedActivitiesCount { get; set; }
    public List<ProjectStatusCount> ProjectsByStatus { get; set; } = new();
    public List<MonthlyActivityCount> ActivitiesByMonth { get; set; } = new();
    public List<ActivityStatusCount> ActivitiesByStatus { get; set; } = new();
    public List<MonthlyProjectCount> NewProjectsByMonth { get; set; } = new();
}

/// <summary>
/// Broj projekata po statusu
/// </summary>
public class ProjectStatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Broj aktivnosti po mesecu
/// </summary>
public class MonthlyActivityCount
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int MonthNum { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Broj aktivnosti po statusu
/// </summary>
public class ActivityStatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Broj novih projekata po mesecu
/// </summary>
public class MonthlyProjectCount
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int MonthNum { get; set; }
    public int Count { get; set; }
}

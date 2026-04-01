using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;

namespace ProjectOrganizer.Api.Services;

/// <summary>
/// Service for calculating project completion statistics
/// </summary>
public class CompletionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompletionService> _logger;

    public CompletionService(ApplicationDbContext context, ILogger<CompletionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculates completion statistics for a project
    /// </summary>
    /// <param name="projektId">Project ID</param>
    /// <returns>Completion statistics or null if no implementation plan</returns>
    public async Task<ProjectCompletionStats?> CalculateProjectCompletionAsync(int projektId)
    {
        // Get all project implementation items for this project
        var projectImplementationItems = await _context.ProjectImplementationItems
            .Where(pii => pii.ProjectId == projektId)
            .Include(pii => pii.CheckLists)
                .ThenInclude(cl => cl.CheckListItem)
            .ToListAsync();

        // If no implementation items, return null (no stats available)
        if (!projectImplementationItems.Any() || 
            !projectImplementationItems.Any(pii => pii.CheckLists.Any()))
        {
            return new ProjectCompletionStats
            {
                HasImplementationPlan = false,
                LastWeekCompletion = 0,
                CurrentCompletion = 0
            };
        }

        // Get all checklist items
        var allCheckListItems = projectImplementationItems
            .SelectMany(pii => pii.CheckLists)
            .ToList();

        if (!allCheckListItems.Any())
        {
            return new ProjectCompletionStats
            {
                HasImplementationPlan = false,
                LastWeekCompletion = 0,
                CurrentCompletion = 0
            };
        }

        var totalItems = allCheckListItems.Count;
        var lastWeekDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        // Calculate completion as of last week
        // Count items that were completed by last week date
        var completedByLastWeek = allCheckListItems
            .Count(item => item.Zavrsen && 
                          item.ZavrsenDatum.HasValue && 
                          item.ZavrsenDatum.Value <= lastWeekDate);

        // Calculate current completion
        // Count items that are currently completed
        var completedNow = allCheckListItems
            .Count(item => item.Zavrsen);

        var lastWeekCompletion = totalItems > 0 
            ? Math.Round((decimal)completedByLastWeek / totalItems * 100, 1) 
            : 0;

        var currentCompletion = totalItems > 0 
            ? Math.Round((decimal)completedNow / totalItems * 100, 1) 
            : 0;

        return new ProjectCompletionStats
        {
            HasImplementationPlan = true,
            LastWeekCompletion = lastWeekCompletion,
            CurrentCompletion = currentCompletion
        };
    }

    /// <summary>
    /// Calculates completion statistics for multiple projects (batch operation)
    /// </summary>
    /// <param name="projektIds">List of project IDs</param>
    /// <returns>Dictionary with project ID as key and completion stats as value</returns>
    public async Task<Dictionary<int, ProjectCompletionStats>> CalculateBatchCompletionAsync(List<int> projektIds)
    {
        var result = new Dictionary<int, ProjectCompletionStats>();
        
        if (!projektIds.Any())
            return result;

        // Get all project implementation items for these projects in one query
        var allProjectImplementationItems = await _context.ProjectImplementationItems
            .Where(pii => projektIds.Contains(pii.ProjectId))
            .Include(pii => pii.CheckLists)
                .ThenInclude(cl => cl.CheckListItem)
            .ToListAsync();

        var lastWeekDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));

        // Group by project ID and calculate stats
        foreach (var projektId in projektIds)
        {
            var projectItems = allProjectImplementationItems
                .Where(pii => pii.ProjectId == projektId)
                .ToList();

            // Get all checklist items for this project
            var checkListItems = projectItems
                .SelectMany(pii => pii.CheckLists)
                .ToList();

            if (!checkListItems.Any())
            {
                result[projektId] = new ProjectCompletionStats
                {
                    HasImplementationPlan = false,
                    LastWeekCompletion = 0,
                    CurrentCompletion = 0
                };
                continue;
            }

            var totalItems = checkListItems.Count;

            // Count completed items by last week
            var completedByLastWeek = checkListItems
                .Count(item => item.Zavrsen && 
                              item.ZavrsenDatum.HasValue && 
                              item.ZavrsenDatum.Value <= lastWeekDate);

            // Count currently completed items
            var completedNow = checkListItems
                .Count(item => item.Zavrsen);

            var lastWeekCompletion = totalItems > 0 
                ? Math.Round((decimal)completedByLastWeek / totalItems * 100, 1) 
                : 0;

            var currentCompletion = totalItems > 0 
                ? Math.Round((decimal)completedNow / totalItems * 100, 1) 
                : 0;

            result[projektId] = new ProjectCompletionStats
            {
                HasImplementationPlan = true,
                LastWeekCompletion = lastWeekCompletion,
                CurrentCompletion = currentCompletion
            };
        }

        return result;
    }
}

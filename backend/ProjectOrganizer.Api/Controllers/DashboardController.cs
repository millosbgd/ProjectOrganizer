using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;
using System.Data;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly UserService _userService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IConfiguration configuration,
        UserService userService,
        ILogger<DashboardController> logger)
    {
        _configuration = configuration;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Vraća sve dashboard statistike za trenutno ulogovanog korisnika
    /// </summary>
    /// <returns>Dashboard statistike sa svim grafikonima</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        try
        {
            // Dohvati trenutnog korisnika
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var stats = new DashboardStats
            {
                ProjectsByStatus = new List<ProjectStatusCount>(),
                ActivitiesByMonth = new List<MonthlyActivityCount>(),
                ActivitiesByStatus = new List<ActivityStatusCount>(),
                NewProjectsByMonth = new List<MonthlyProjectCount>()
            };

            // Dohvati connection string na osnovu environment-a
            var environment = _configuration["Environment"] ?? "Local";
            var connectionString = environment == "Production"
                ? _configuration.GetConnectionString("Production")
                : _configuration.GetConnectionString("Local");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_GetDashboardStats", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", currentUser.Id);
                    command.CommandTimeout = 30;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: Osnovne metrike (ActiveProjectsCount, UnfinishedActivitiesCount)
                        if (await reader.ReadAsync())
                        {
                            stats.ActiveProjectsCount = reader.GetInt32(reader.GetOrdinal("ActiveProjectsCount"));
                            stats.UnfinishedActivitiesCount = reader.GetInt32(reader.GetOrdinal("UnfinishedActivitiesCount"));
                        }

                        // Result Set 2: Projekti po statusu
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.ProjectsByStatus.Add(new ProjectStatusCount
                                {
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    Count = reader.GetInt32(reader.GetOrdinal("Count"))
                                });
                            }
                        }

                        // Result Set 3: Aktivnosti po mesecima
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.ActivitiesByMonth.Add(new MonthlyActivityCount
                                {
                                    Month = reader.GetString(reader.GetOrdinal("Month")),
                                    Year = reader.GetInt32(reader.GetOrdinal("Year")),
                                    MonthNum = reader.GetInt32(reader.GetOrdinal("MonthNum")),
                                    Count = reader.GetInt32(reader.GetOrdinal("Count"))
                                });
                            }
                        }

                        // Result Set 4: Aktivnosti po statusu
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.ActivitiesByStatus.Add(new ActivityStatusCount
                                {
                                    Status = reader.GetString(reader.GetOrdinal("Status")),
                                    Count = reader.GetInt32(reader.GetOrdinal("Count"))
                                });
                            }
                        }

                        // Result Set 5: Novi projekti po mesecima
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                stats.NewProjectsByMonth.Add(new MonthlyProjectCount
                                {
                                    Month = reader.GetString(reader.GetOrdinal("Month")),
                                    Year = reader.GetInt32(reader.GetOrdinal("Year")),
                                    MonthNum = reader.GetInt32(reader.GetOrdinal("MonthNum")),
                                    Count = reader.GetInt32(reader.GetOrdinal("Count"))
                                });
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Dashboard stats retrieved successfully for user {UserId}", currentUser.Id);
            return Ok(stats);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error while retrieving dashboard stats");
            return StatusCode(500, "Greška pri učitavanju dashboard statistike.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving dashboard stats");
            return StatusCode(500, "Došlo je do greške pri učitavanju podataka.");
        }
    }

    /// <summary>
    /// Vraća samo osnovne metrike (brojeve) za dashboard
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult> GetBasicMetrics()
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var environment = _configuration["Environment"] ?? "Local";
            var connectionString = environment == "Production"
                ? _configuration.GetConnectionString("Production")
                : _configuration.GetConnectionString("Local");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_GetDashboardStats", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", currentUser.Id);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Samo prvi result set (osnovne metrike)
                        if (await reader.ReadAsync())
                        {
                            var metrics = new
                            {
                                activeProjectsCount = reader.GetInt32(reader.GetOrdinal("ActiveProjectsCount")),
                                unfinishedActivitiesCount = reader.GetInt32(reader.GetOrdinal("UnfinishedActivitiesCount"))
                            };

                            return Ok(metrics);
                        }
                    }
                }
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving basic metrics");
            return StatusCode(500, "Došlo je do greške.");
        }
    }
}

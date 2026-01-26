using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using ProjectOrganizer.Api.Services;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectPermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<ProjectPermissionsController> _logger;

    public ProjectPermissionsController(
        ApplicationDbContext context,
        UserService userService,
        ILogger<ProjectPermissionsController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    // GET: api/projectpermissions/projekat/{projekatId}
    [HttpGet("projekat/{projekatId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetPermissionsForProjekat(int projekatId)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            // Only admins or users with Admin permission on the project can view permissions
            if (currentUser.Role != "Admin")
            {
                var hasAdminPermission = await _context.ProjectPermissions
                    .AnyAsync(p => p.ProjekatId == projekatId && 
                                   p.UserId == currentUser.Id && 
                                   p.PermissionLevel == "Admin");
                
                if (!hasAdminPermission)
                {
                    return Forbid();
                }
            }

            var permissions = await _context.ProjectPermissions
                .Include(p => p.User)
                .Where(p => p.ProjekatId == projekatId)
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    p.ProjekatId,
                    p.PermissionLevel,
                    p.CreatedAt,
                    User = new
                    {
                        p.User!.Id,
                        p.User.Email,
                        p.User.Name
                    }
                })
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting permissions for projekat {projekatId}");
            return StatusCode(500, "Error retrieving permissions");
        }
    }

    // POST: api/projectpermissions
    [HttpPost]
    public async Task<ActionResult<ProjectPermission>> AddPermission([FromBody] ProjectPermission permission)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            // Only admins can add permissions
            if (currentUser.Role != "Admin")
            {
                var hasAdminPermission = await _context.ProjectPermissions
                    .AnyAsync(p => p.ProjekatId == permission.ProjekatId && 
                                   p.UserId == currentUser.Id && 
                                   p.PermissionLevel == "Admin");
                
                if (!hasAdminPermission)
                {
                    return Forbid();
                }
            }

            // Check if permission already exists
            var existing = await _context.ProjectPermissions
                .FirstOrDefaultAsync(p => p.UserId == permission.UserId && p.ProjekatId == permission.ProjekatId);

            if (existing != null)
            {
                return BadRequest("Permission already exists for this user and project");
            }

            permission.CreatedAt = DateTime.UtcNow;
            permission.CreatedBy = currentUser.Id;

            _context.ProjectPermissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Permission added for user {permission.UserId} on project {permission.ProjekatId} by {currentUser.Email}");

            return CreatedAtAction(nameof(GetPermissionsForProjekat), new { projekatId = permission.ProjekatId }, permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding permission");
            return StatusCode(500, "Error adding permission");
        }
    }

    // PUT: api/projectpermissions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePermission(int id, [FromBody] ProjectPermission updatedPermission)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            var permission = await _context.ProjectPermissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            // Only admins can update permissions
            if (currentUser.Role != "Admin")
            {
                var hasAdminPermission = await _context.ProjectPermissions
                    .AnyAsync(p => p.ProjekatId == permission.ProjekatId && 
                                   p.UserId == currentUser.Id && 
                                   p.PermissionLevel == "Admin");
                
                if (!hasAdminPermission)
                {
                    return Forbid();
                }
            }

            permission.PermissionLevel = updatedPermission.PermissionLevel;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Permission {id} updated to {permission.PermissionLevel} by {currentUser.Email}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating permission {id}");
            return StatusCode(500, "Error updating permission");
        }
    }

    // DELETE: api/projectpermissions/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePermission(int id)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            var permission = await _context.ProjectPermissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            // Only admins can delete permissions
            if (currentUser.Role != "Admin")
            {
                var hasAdminPermission = await _context.ProjectPermissions
                    .AnyAsync(p => p.ProjekatId == permission.ProjekatId && 
                                   p.UserId == currentUser.Id && 
                                   p.PermissionLevel == "Admin");
                
                if (!hasAdminPermission)
                {
                    return Forbid();
                }
            }

            _context.ProjectPermissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Permission {id} deleted by {currentUser.Email}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting permission {id}");
            return StatusCode(500, "Error deleting permission");
        }
    }

    // GET: api/projectpermissions/my-projects - Get projects user has access to
    [HttpGet("my-projects")]
    public async Task<ActionResult<IEnumerable<int>>> GetMyProjects()
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            // Admins have access to all projects
            if (currentUser.Role == "Admin")
            {
                var allProjectIds = await _context.Projekti.Select(p => p.Id).ToListAsync();
                return Ok(allProjectIds);
            }

            var projectIds = await _context.ProjectPermissions
                .Where(p => p.UserId == currentUser.Id)
                .Select(p => p.ProjekatId)
                .ToListAsync();

            return Ok(projectIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's projects");
            return StatusCode(500, "Error retrieving projects");
        }
    }
}

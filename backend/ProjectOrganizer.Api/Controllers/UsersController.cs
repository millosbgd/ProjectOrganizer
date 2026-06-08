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
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context, 
        UserService userService,
        ILogger<UsersController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    // GET: api/users/me - Get current user info
    [HttpGet("me")]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        try
        {
            var user = await _userService.EnsureUserExistsAsync(User);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, "Error retrieving user information");
        }
    }

    // GET: api/users/me/menu-permissions - Get visible menu keys for current user
    [HttpGet("me/menu-permissions")]
    public async Task<ActionResult<IEnumerable<string>>> GetCurrentUserMenuPermissions()
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var menuKeys = await _context.UserMenuPermissions
                .Where(p => p.UserId == currentUser.Id)
                .Select(p => p.MenuKey)
                .Distinct()
                .OrderBy(menuKey => menuKey)
                .ToListAsync();

            return Ok(menuKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user menu permissions");
            return StatusCode(500, "Error retrieving menu permissions");
        }
    }

    // GET: api/users - Get all users (Admin only)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            if (currentUser.Role != "Admin")
            {
                return Forbid();
            }

            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, "Error retrieving users");
        }
    }

    // PUT: api/users/{id} - Update user (Admin only)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            if (currentUser.Role != "Admin")
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Role = updatedUser.Role;
            user.IsActive = updatedUser.IsActive;
            user.Name = updatedUser.Name;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {user.Email} updated by {currentUser.Email}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating user {id}");
            return StatusCode(500, "Error updating user");
        }
    }

    // DELETE: api/users/{id} - Deactivate user (Admin only)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);
            
            if (currentUser.Role != "Admin")
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Can't deactivate yourself
            if (user.Id == currentUser.Id)
            {
                return BadRequest("Cannot deactivate your own account");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {user.Email} deactivated by {currentUser.Email}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deactivating user {id}");
            return StatusCode(500, "Error deactivating user");
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Services;

namespace ProjectOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(ApplicationDbContext context, UserService userService, ILogger<NotificationsController> logger)
    {
        _context = context;
        _userService = userService;
        _logger = logger;
    }

    // GET: api/Notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var notifications = await _context.Notifications
            .Where(n => n.UserId == currentUser.Id && !n.Dismissed)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(notifications);
    }

    // GET: api/Notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var currentUser = await _userService.EnsureUserExistsAsync(User);

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.Id && !n.IsRead && !n.Dismissed);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUnreadCount");
            return StatusCode(500);
        }
    }

    // PUT: api/Notifications/5/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/Notifications/5/dismiss
    [HttpPut("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(int id)
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

        if (notification == null)
            return NotFound();

        notification.Dismissed = true;
        if (!notification.IsRead)
        {
            notification.IsRead = true;
        }
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PUT: api/Notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var currentUser = await _userService.EnsureUserExistsAsync(User);

        await _context.Notifications
            .Where(n => n.UserId == currentUser.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return NoContent();
    }
}

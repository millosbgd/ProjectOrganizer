using Microsoft.EntityFrameworkCore;
using ProjectOrganizer.Api.Data;
using ProjectOrganizer.Api.Models;
using System.Security.Claims;

namespace ProjectOrganizer.Api.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User> EnsureUserExistsAsync(ClaimsPrincipal userPrincipal)
    {
        var auth0Id = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(auth0Id))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

        if (user == null)
        {
            // Auto-register new user
            var email = userPrincipal.FindFirstValue(ClaimTypes.Email) ?? 
                       userPrincipal.FindFirstValue("email") ?? 
                       $"{auth0Id}@unknown.com";
            
            var name = userPrincipal.FindFirstValue(ClaimTypes.Name) ?? 
                      userPrincipal.FindFirstValue("name") ?? 
                      "Unknown User";

            user = new User
            {
                Auth0Id = auth0Id,
                Email = email,
                Name = name,
                Role = "User",
                IsActive = true,
                LastLogin = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"New user registered: {email} (Auth0Id: {auth0Id})");
        }
        else
        {
            // Update last login
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return user;
    }
}

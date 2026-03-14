using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjectOrganizer.Api.Hubs;

/// <summary>
/// SignalR hub za real-time dostavu notifikacija.
/// Identifikacija korisnika se oslanja na NameIdentifier claim (Auth0 sub),
/// koji ASP.NET Core-ov JWT middleware automatski mapira na Context.UserIdentifier.
/// Slanje: _hubContext.Clients.User(auth0Id).SendAsync("ReceiveNotification", payload)
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    // Hub nema custom logiku - SVA logika slanja je u NotificationService.
    // Ovde možeš proširiti npr. grupama (timovi, projekti) u budućnosti.
}

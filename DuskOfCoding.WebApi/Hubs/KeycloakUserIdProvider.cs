using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DuskOfCoding.WebApi.Hubs;

/// <summary>
/// Maps the Keycloak 'sub' claim (NameIdentifier) to a SignalR user ID,
/// enabling Clients.User(userId) to target specific authenticated users.
/// </summary>
public sealed class KeycloakUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

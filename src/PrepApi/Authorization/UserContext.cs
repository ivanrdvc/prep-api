using System.Security.Claims;

using PrepApi.Users;

namespace PrepApi.Authorization;

public interface IUserContext
{
    public User? User { get; set; }
    ClaimsPrincipal Principal { get; set; }

    /// <summary>
    /// Internal user ID. Guaranteed to be available when RequireCurrentUser() policy is used.
    /// </summary>
    Guid InternalId { get; }

    /// <summary>
    /// External ID from Auth0 (sub claim). Always available for authenticated users.
    /// </summary>
    string ExternalId { get; }

    bool IsAdmin { get; }
}

public class UserContext : IUserContext
{
    public User? User { get; set; }
    public ClaimsPrincipal Principal { get; set; } = null!;
    public Guid InternalId => User!.Id;
    public string ExternalId => Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public bool IsAdmin => Principal.IsInRole("admin");
}
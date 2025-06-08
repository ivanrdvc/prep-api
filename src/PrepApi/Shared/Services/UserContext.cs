using System.Security.Claims;

using PrepApi.Users;

namespace PrepApi.Shared.Services;

public interface IUserContext
{
    public User? User { get; set; }
    ClaimsPrincipal Principal { get; set; }
    Guid? InternalId { get; }
}

public class UserContext : IUserContext
{
    public User? User { get; set; }
    public ClaimsPrincipal Principal { get; set; } = null!;
    public Guid? InternalId => User?.Id;
}
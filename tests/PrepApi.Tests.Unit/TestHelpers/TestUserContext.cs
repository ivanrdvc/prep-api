using System.Security.Claims;

using PrepApi.Authorization;
using PrepApi.Tests.Integration.TestHelpers;
using PrepApi.Users;

namespace PrepApi.Tests.Unit.TestHelpers;

public class TestUserContext : IUserContext
{
    public User? User { get; set; }
    public ClaimsPrincipal Principal { get; set; } = new();
    public Guid InternalId => User!.Id;
    public string ExternalId => Principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public bool IsAdmin => Principal.IsInRole("admin");

    public static IUserContext Authenticated(string userId = "test-user-id") =>
        new TestUserContext
        {
            User = new User
            {
                Id = TestConstants.TestUserId,
                ExternalId = userId
            },
            Principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId)
            ], "test"))
        };

    public static IUserContext AuthenticatedAdmin(string userId = "admin-user-id") =>
        new TestUserContext
        {
            User = new User
            {
                Id = TestConstants.TestUserId,
                ExternalId = userId
            },
            Principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "admin")
            ], "test"))
        };

    public static IUserContext Unauthenticated() =>
        new TestUserContext
        {
            User = null,
            Principal = new ClaimsPrincipal()
        };
}
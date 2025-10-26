using System.Security.Claims;

using PrepApi.Shared.Services;
using PrepApi.Tests.Integration.TestHelpers;
using PrepApi.Users;

namespace PrepApi.Tests.Unit.TestHelpers;

public class TestUserContext : IUserContext
{
    public User? User { get; set; }
    public ClaimsPrincipal Principal { get; set; } = new();
    public Guid? InternalId => User?.Id;

    private TestUserContext() { }

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
}
using PrepApi.Shared.Services;

namespace PrepApi.Tests.Unit.Helpers;

public class TestUserContext : IUserContext
{
    public bool IsAuthenticated { get; init; } = true;
    public string? ExternalId { get; init; }

    private TestUserContext() { }

    public static IUserContext Authenticated(string userId = "test-user-id") =>
        new TestUserContext { IsAuthenticated = true, ExternalId = userId };

    public static IUserContext Anonymous() =>
        new TestUserContext { IsAuthenticated = false, ExternalId = null };
}
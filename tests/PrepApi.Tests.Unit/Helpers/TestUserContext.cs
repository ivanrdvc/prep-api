namespace PrepApi.Tests.Unit.Helpers;

public class TestUserContext : IUserContext
{
    public bool IsAuthenticated { get; set; } = true;
    public string? UserId { get; set; }

    private TestUserContext() { }

    public static IUserContext Authenticated(string userId = "test-user-id") =>
        new TestUserContext { IsAuthenticated = true, UserId = userId };

    public static IUserContext Anonymous() =>
        new TestUserContext { IsAuthenticated = false, UserId = null };
}
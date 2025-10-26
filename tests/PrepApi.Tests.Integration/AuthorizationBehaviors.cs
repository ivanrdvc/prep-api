using System.Net;

using PrepApi.Tests.Integration.TestHelpers;

namespace PrepApi.Tests.Integration;

public class AuthorizationBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    [Fact]
    public async Task Request_WithNoJwt_Returns401Unauthorized()
    {
        // Arrange
        var client = factory.CreateUnauthenticatedClient();

        // Act - GET /api/users/me requires authentication
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidJwtButUserNotInDb_Returns403Forbidden()
    {
        // Arrange
        var client = factory.CreateClient();
        // Use a different external ID that doesn't exist in DB
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.AuthenticationHeaderName);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeaderName, "non-existent-user-id");

        // Act - GET /api/users/me requires RequireCurrentUser (user must exist in DB)
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithValidJwtAndUserInDb_Returns200Ok()
    {
        // Arrange
        var client = factory.CreateClient();
        // Default client has TestUserExternalId which exists in DB (seeded in InitializeAsync)

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
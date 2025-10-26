using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using NSubstitute;

using PrepApi.Authorization;
using PrepApi.Tests.Integration.TestHelpers;
using PrepApi.Tests.Unit.TestHelpers;
using PrepApi.Users;
using PrepApi.Users.Requests;

namespace PrepApi.Tests.Unit.Users;

public class UserEndpointsTests
{
    private readonly FakeDb _fakeDb;
    private readonly IUserContext _userContext;

    public UserEndpointsTests()
    {
        _userContext = TestUserContext.Authenticated();
        _fakeDb = new FakeDb(_userContext);
    }

    [Fact]
    public async Task GetCurrentUser_UserExistsInDb_ReturnsUserDto()
    {
        // Arrange - User is already in context via TestUserContext.Authenticated()

        // Act
        var result = await UserEndpoints.GetCurrentUser(_userContext);

        // Assert
        var okResult = Assert.IsType<Ok<UserDto>>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(TestConstants.TestUserId, okResult.Value.Id);
    }

    [Fact]
    public async Task GetCurrentUser_UsesUserContextDirectly_NoDatabaseQuery()
    {
        // Arrange
        var userContext = TestUserContext.Authenticated();

        // Act
        var result = await UserEndpoints.GetCurrentUser(userContext);

        // Assert
        var okResult = Assert.IsType<Ok<UserDto>>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CreateCurrentUser_ValidJwtAndUserDoesNotExist_CreatesUser()
    {
        // Arrange
        await using var db = _fakeDb.CreateDbContext();
        var userContext = TestUserContext.Unauthenticated();
        userContext.User = null;

        var httpContextAccessor = CreateHttpContextAccessor("new-user-external-id");

        // Act
        var result = await UserEndpoints.CreateCurrentUser(db, userContext, httpContextAccessor);

        // Assert
        var createdResult = Assert.IsType<Created<UserDto>>(result.Result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal("/api/users/me", createdResult.Location);

        // Verify user was added to database
        var userInDb = await db.Users.FindAsync(createdResult.Value.Id);
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task CreateCurrentUser_ValidJwtButUserAlreadyExists_ReturnsConflict()
    {
        // Arrange
        await using var db = _fakeDb.CreateDbContext();
        var userContext = TestUserContext.Authenticated(); // User already exists
        var httpContextAccessor = CreateHttpContextAccessor("test-user-id");

        // Act
        var result = await UserEndpoints.CreateCurrentUser(db, userContext, httpContextAccessor);

        // Assert
        Assert.IsType<Conflict>(result.Result);
    }

    [Fact]
    public async Task CreateCurrentUser_NoJwt_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var db = _fakeDb.CreateDbContext();
        var userContext = TestUserContext.Unauthenticated();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => UserEndpoints.CreateCurrentUser(db, userContext, httpContextAccessor));
    }

    [Fact]
    public async Task CreateCurrentUser_UnauthenticatedHttpContext_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        await using var db = _fakeDb.CreateDbContext();
        var userContext = TestUserContext.Unauthenticated();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal() // Not authenticated
        };

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => UserEndpoints.CreateCurrentUser(db, userContext, httpContextAccessor));
    }


    [Fact]
    public async Task UpdateCurrentUser_UserExistsInDb_UpdatesSuccessfully()
    {
        // Arrange
        await using var db = _fakeDb.CreateDbContext();
        var user = _userContext.User!;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PreferredUnits = PreferredUnits.Imperial
        };

        // Act
        var result = await UserEndpoints.UpdateCurrentUser(request, db, _userContext);

        // Assert
        Assert.IsType<NoContent>(result.Result);

        // Verify updates
        var updatedUser = await db.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("John", updatedUser.FirstName);
        Assert.Equal("Doe", updatedUser.LastName);
        Assert.Equal(PreferredUnits.Imperial, updatedUser.PreferredUnits);
    }

    [Fact]
    public async Task UpdateCurrentUser_UsesUserContextDirectly_NoDatabaseQuery()
    {
        // Arrange
        await using var db = _fakeDb.CreateDbContext();
        var user = _userContext.User!;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            PreferredUnits = PreferredUnits.Metric
        };

        // Act
        var result = await UserEndpoints.UpdateCurrentUser(request, db, _userContext);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }


    private static IHttpContextAccessor CreateHttpContextAccessor(string externalId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, externalId)
        };

        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return httpContextAccessor;
    }
}
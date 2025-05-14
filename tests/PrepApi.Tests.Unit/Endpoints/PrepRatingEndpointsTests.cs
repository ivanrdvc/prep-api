using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;

using PrepApi.Contracts;
using PrepApi.Endpoints;
using PrepApi.Tests.Unit.Helpers;

namespace PrepApi.Tests.Unit.Endpoints;

public class PrepRatingEndpointsTests
{
    private readonly IUserContext _userContext;
    private readonly FakeDb _fakeDb;
    private readonly IValidator<UpsertPrepRatingRequest> _validator;

    public PrepRatingEndpointsTests()
    {
        _userContext = TestUserContext.Authenticated();
        _fakeDb = new FakeDb(_userContext);
        _validator = new UpsertPrepRatingRequestValidator();
    }

    [Fact]
    public async Task CreatePrepRating_InvalidRequest_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 0 };

        // Act
        var result = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async Task CreatePrepRating_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 5 };
        var anonUser = TestUserContext.Anonymous();
        // No need to mock validator, use real one

        // Act
        var result = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, anonUser, _validator);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task CreatePrepRating_PrepNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var request = new UpsertPrepRatingRequest { OverallRating = 5 };

        // Act
        var result =
            await PrepRatingEndpoints.CreatePrepRating(Guid.NewGuid(), request, context, _userContext, _validator);

        // Assert
        Assert.IsType<NotFound<string>>(result.Result);
    }

    [Fact]
    public async Task CreatePrepRating_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 5, Liked = true };

        // Act
        var result = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);

        // Assert
        Assert.IsType<Created<Guid>>(result.Result);
    }

    [Fact]
    public async Task UpdatePrepRating_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 5 };
        var anonUser = TestUserContext.Anonymous();

        // Act
        var result = await PrepRatingEndpoints.UpdatePrepRating(prep.Id, Guid.NewGuid(), request, context, anonUser);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePrepRating_NotFound_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 5 };

        // Act
        var result =
            await PrepRatingEndpoints.UpdatePrepRating(prep.Id, Guid.NewGuid(), request, context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdatePrepRating_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var ratingRequest = new UpsertPrepRatingRequest { OverallRating = 5, Liked = true };
        // Create rating
        var createResult =
            await PrepRatingEndpoints.CreatePrepRating(prep.Id, ratingRequest, context, _userContext, _validator);
        var createdId = ((Created<Guid>)createResult.Result).Value;

        // Act
        var updateRequest = new UpsertPrepRatingRequest { OverallRating = 4, Liked = false };
        var result =
            await PrepRatingEndpoints.UpdatePrepRating(prep.Id, createdId, updateRequest, context, _userContext);
        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task GetPrepRatings_NoneExist_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        // Act
        var result = await PrepRatingEndpoints.GetPrepRatings(prep.Id, context);
        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetPrepRatings_Exist_ReturnsOkWithRatings()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var ratingRequest = new UpsertPrepRatingRequest { OverallRating = 5, Liked = true };
        await PrepRatingEndpoints.CreatePrepRating(prep.Id, ratingRequest, context, _userContext, _validator);
        
        // Act
        var result = await PrepRatingEndpoints.GetPrepRatings(prep.Id, context);
        // Assert
        var okResult = Assert.IsType<Ok<List<PrepRatingDto>>>(result.Result);
        Assert.Single(okResult.Value);
        Assert.Equal(5, okResult.Value[0].OverallRating);
    }
}
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
        var request = new UpsertPrepRatingRequest { OverallRating = 0, Dimensions = new() };

        // Act
        var result = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }
    
    [Fact]
    public async Task CreatePrepRating_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        await _fakeDb.SeedDefaultRatingDimensionsAsync(context);
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest
        {
            OverallRating = 5,
            Liked = true,
            Dimensions = new Dictionary<string, int> { { "taste", 5 }, { "texture", 4 } },
            WhatWorkedWell = "Great taste!",
            WhatToChange = "None",
            AdditionalNotes = "Would make again."
        };

        // Act
        var result = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);

        // Assert
        Assert.IsType<Created<Guid>>(result.Result);
    }

    [Fact]
    public async Task CreatePrepRating_DuplicateRating_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 5, Liked = true, Dimensions = new() };

        // Act
        var firstResult = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);
        var secondResult = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);

        // Assert
        Assert.IsType<Created<Guid>>(firstResult.Result);
        Assert.IsType<ValidationProblem>(secondResult.Result);
    }

    [Fact]
    public async Task CreatePrepRating_WithInvalidDimension_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        await _fakeDb.SeedDefaultRatingDimensionsAsync(context);
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest
        {
            OverallRating = 5,
            Liked = true,
            Dimensions = new Dictionary<string, int> { { "invalid_dim", 3 } },
            WhatWorkedWell = "Test",
            WhatToChange = "Test",
            AdditionalNotes = "Test"
        };

        // Act
        var result = await PrepRatingEndpoints.CreatePrepRating(prep.Id, request, context, _userContext, _validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async Task UpdatePrepRating_NotFound_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = new UpsertPrepRatingRequest { OverallRating = 5, Dimensions = new() };

        // Act
        var result = await PrepRatingEndpoints.UpdatePrepRating(prep.Id, Guid.NewGuid(), request, context, _userContext,
            _validator);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdatePrepRating_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        await _fakeDb.SeedDefaultRatingDimensionsAsync(context);
        var prep = await _fakeDb.SeedPrepAsync(context);
        var rating = await _fakeDb.SeedPrepRatingAsync(context, prep.Id, _userContext.UserId);

        // Act
        var updateRequest = new UpsertPrepRatingRequest
        {
            OverallRating = 4,
            Liked = false,
            Dimensions = new Dictionary<string, int> { { "appearance", 3 } },
            WhatWorkedWell = "Nice look",
            WhatToChange = "Improve texture",
            AdditionalNotes = "Test update."
        };
        var result = await PrepRatingEndpoints.UpdatePrepRating(prep.Id, rating.Id, updateRequest, context, _userContext,
            _validator);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task UpdatePrepRating_InvalidRequest_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var rating = await _fakeDb.SeedPrepRatingAsync(context, prep.Id, _userContext.UserId);
        var invalidRequest = new UpsertPrepRatingRequest { OverallRating = 0, Liked = true, Dimensions = new() };

        // Act
        var result = await PrepRatingEndpoints.UpdatePrepRating(prep.Id, rating.Id, invalidRequest, context, _userContext,
            _validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
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
        await _fakeDb.SeedPrepRatingAsync(context, prep.Id, _userContext.UserId);

        // Act
        var result = await PrepRatingEndpoints.GetPrepRatings(prep.Id, context);

        // Assert
        var okResult = Assert.IsType<Ok<List<PrepRatingDto>>>(result.Result);
        Assert.Single(okResult.Value!);
    }
}
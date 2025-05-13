using Microsoft.AspNetCore.Http.HttpResults;

using NSubstitute;

using PrepApi.Contracts;
using PrepApi.Data;
using PrepApi.Endpoints;
using PrepApi.Tests.Unit.Helpers;

namespace PrepApi.Tests.Unit.Endpoints;

public class PrepEndpointsTests
{
    private readonly IUserContext _userContext;
    private readonly FakeDb _fakeDb;

    public PrepEndpointsTests()
    {
        _userContext = TestUserContext.Authenticated();
        _fakeDb = new FakeDb(_userContext);
    }

    [Fact]
    public async Task GetPrep_NotExists_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();

        // Act
        var result = await PrepEndpoints.GetPrep(Guid.NewGuid(), context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetPrep_Exists_ReturnsOk()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);

        // Act
        var result = await PrepEndpoints.GetPrep(prep.Id, context, _userContext);

        // Assert
        Assert.IsType<Ok<PrepDto>>(result.Result);
    }

    [Fact]
    public async Task CreatePrep_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);
        var request = _fakeDb.CreateUpsertPrepRequest(context, recipe.Id);
        var validator = new UpsertPrepRequestValidator();

        // Act
        var result = await PrepEndpoints.CreatePrep(request, context, _userContext, validator);

        // Assert
        Assert.IsType<Created<Guid>>(result.Result);
    }

    [Fact]
    public async Task CreatePrep_InvalidRequest_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();

        var invalidRequest = new UpsertPrepRequest
        {
            RecipeId = Guid.Empty,
            SummaryNotes = new string('x', 3000),
            PrepTimeMinutes = -10,
            CookTimeMinutes = -5,
            Steps = [new StepDto { Description = "", Order = 1 }],
            PrepIngredients =
            [
                new PrepIngredientInputDto
                {
                    IngredientId = Guid.NewGuid(),
                    Quantity = 0,
                    Unit = PrepApi.Data.Unit.Gram,
                    Notes = new string('x', 600)
                }
            ]
        };

        var validator = new UpsertPrepRequestValidator();

        // Act
        var result = await PrepEndpoints.CreatePrep(invalidRequest, context, _userContext, validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async Task CreatePrep_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);
        var request = _fakeDb.CreateUpsertPrepRequest(context, recipe.Id);
        var validator = new UpsertPrepRequestValidator();
        var anonUserContext = TestUserContext.Anonymous();

        // Act
        var result = await PrepEndpoints.CreatePrep(request, context, anonUserContext, validator);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_Exists_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = _fakeDb.CreateUpsertPrepRequest(context, prep.RecipeId);
        var validator = new UpsertPrepRequestValidator();

        // Act
        var result = await PrepEndpoints.UpdatePrep(prep.Id, request, context, _userContext, validator);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_NotExists_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);
        var request = _fakeDb.CreateUpsertPrepRequest(context, recipe.Id);
        var validator = new UpsertPrepRequestValidator();

        // Act
        var result = await PrepEndpoints.UpdatePrep(Guid.NewGuid(), request, context, _userContext, validator);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_DifferentUser_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = _fakeDb.CreateUpsertPrepRequest(context, prep.RecipeId);
        var validator = new UpsertPrepRequestValidator();

        var differentUserContext = Substitute.For<IUserContext>();
        differentUserContext.UserId.Returns(Guid.NewGuid().ToString());

        // Act
        var result = await PrepEndpoints.UpdatePrep(prep.Id, request, context, differentUserContext, validator);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_InvalidIngredients_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);

        var request = _fakeDb.CreateUpsertPrepRequest(context, prep.RecipeId);

        request.PrepIngredients.Add(new PrepIngredientInputDto
        {
            IngredientId = Guid.NewGuid(),
            Quantity = 1,
            Unit = PrepApi.Data.Unit.Gram
        });

        var validator = new UpsertPrepRequestValidator();

        // Act
        var result = await PrepEndpoints.UpdatePrep(prep.Id, request, context, _userContext, validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async Task DeletePrep_Exists_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);

        // Act
        var result = await PrepEndpoints.DeletePrep(prep.Id, context, _userContext);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task DeletePrep_NotExists_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();

        // Act
        var result = await PrepEndpoints.DeletePrep(Guid.NewGuid(), context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetPrepsByRecipe_ValidRequest_ReturnsPaginatedResult()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);
        await _fakeDb.SeedPrepAsync(context, recipeId: recipe.Id);
        var request = new PaginationRequest { PageIndex = 0, PageSize = 10 };

        // Act
        var result = await PrepEndpoints.GetPrepsByRecipe(recipe.Id, request, context, _userContext);

        // Assert
        Assert.IsType<Ok<PaginatedItems<PrepSummaryDto>>>(result.Result);
    }

    [Fact]
    public async Task GetPrepsByRecipe_PageSizeZero_ReturnsEmptyData()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);
        await _fakeDb.SeedPrepAsync(context, recipeId: recipe.Id);
        var request = new PaginationRequest { PageIndex = 0, PageSize = 0 };

        // Act
        var result = await PrepEndpoints.GetPrepsByRecipe(recipe.Id, request, context, _userContext);

        // Assert
        Assert.Empty(Assert.IsType<Ok<PaginatedItems<PrepSummaryDto>>>(result.Result).Value!.Data);
    }

    [Theory]
    [InlineData(SortOrder.desc, 1)]
    [InlineData(SortOrder.asc, 0)]
    public async Task GetPrepsByRecipe_SortOrder_ReturnsItemsInCorrectOrder(
        SortOrder sortOrder,
        int firstExpectedIndex)
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);

        var preps = new List<Prep>();

        var older = await _fakeDb.SeedPrepAsync(context, recipeId: recipe.Id);
        older.CreatedAt = DateTime.UtcNow.AddDays(-2);
        preps.Add(older);
        var newer = await _fakeDb.SeedPrepAsync(context, recipeId: recipe.Id);
        newer.CreatedAt = DateTime.UtcNow;
        preps.Add(newer);

        await context.SaveChangesAsync();

        var request = new PaginationRequest
        {
            PageIndex = 0,
            PageSize = 10,
            SortOrder = sortOrder
        };

        // Act
        var result = await PrepEndpoints.GetPrepsByRecipe(recipe.Id, request, context, _userContext);

        // Assert
        var data = Assert.IsType<Ok<PaginatedItems<PrepSummaryDto>>>(result.Result).Value!.Data.ToList();
        Assert.Equal(preps[firstExpectedIndex].Id, data[0].Id);
    }
}
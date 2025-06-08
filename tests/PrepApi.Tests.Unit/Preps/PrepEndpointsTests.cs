using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using NSubstitute;

using PrepApi.Data;
using PrepApi.Preps;
using PrepApi.Preps.Entities;
using PrepApi.Preps.Requests;
using PrepApi.Shared.Dtos;
using PrepApi.Shared.Requests;
using PrepApi.Shared.Services;
using PrepApi.Tests.Unit.Helpers;

namespace PrepApi.Tests.Unit.Preps;

public class PrepEndpointsTests
{
    private readonly IUserContext _userContext;
    private readonly FakeDb _fakeDb;
    private readonly IValidator<UpsertPrepRequest> _validator;
    private readonly PrepService _prepService;

    public PrepEndpointsTests()
    {
        _userContext = TestUserContext.Authenticated();
        _fakeDb = new FakeDb(_userContext);
        _validator = new UpsertPrepRequestValidator();
        _prepService = new PrepService();
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
        var request = CreateUpsertPrepRequest(context, recipe.Id);

        // Act
        var result = await PrepEndpoints.CreatePrep(request, context, _userContext, _prepService, _validator);

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
                    Unit = Shared.Entities.Unit.Gram,
                    Notes = new string('x', 600)
                }
            ]
        };

        // Act
        var result = await PrepEndpoints.CreatePrep(invalidRequest, context, _userContext, _prepService, _validator);

        // Assert
        Assert.IsType<ValidationProblem>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_Exists_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = CreateUpsertPrepRequest(context, prep.RecipeId);

        // Act
        var result = await PrepEndpoints.UpdatePrep(prep.Id, request, context, _userContext, _prepService, _validator);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_NotExists_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await _fakeDb.SeedRecipeAsync(context);
        var request = CreateUpsertPrepRequest(context, recipe.Id);

        // Act
        var result = await PrepEndpoints.UpdatePrep(Guid.NewGuid(), request, context, _userContext, _prepService, _validator);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_DifferentUser_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);
        var request = CreateUpsertPrepRequest(context, prep.RecipeId);

        var differentUserContext = Substitute.For<IUserContext>();
        differentUserContext.ExternalId.Returns(Guid.NewGuid().ToString());

        // Act
        var result = await PrepEndpoints.UpdatePrep(prep.Id, request, context, differentUserContext, _prepService, _validator);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdatePrep_InvalidIngredients_ReturnsValidationProblem()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var prep = await _fakeDb.SeedPrepAsync(context);

        var request = CreateUpsertPrepRequest(context, prep.RecipeId);

        request.PrepIngredients.Add(new PrepIngredientInputDto
        {
            IngredientId = Guid.NewGuid(),
            Quantity = 1,
            Unit = Shared.Entities.Unit.Gram
        });

        // Act
        var result = await PrepEndpoints.UpdatePrep(prep.Id, request, context, _userContext, _prepService, _validator);

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

    private UpsertPrepRequest CreateUpsertPrepRequest(PrepDb context, Guid recipeId)
    {
        var recipe = context.Recipes.Include(r => r.RecipeIngredients).FirstOrDefault(r => r.Id == recipeId);
        var ingredientId = recipe?.RecipeIngredients.FirstOrDefault()?.IngredientId ?? Guid.NewGuid();
        return new UpsertPrepRequest
        {
            RecipeId = recipeId,
            SummaryNotes = "Prep notes",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Steps = [new() { Description = "Step 1", Order = 1 }],
            PrepIngredients = [new() { IngredientId = ingredientId, Quantity = 100, Unit = Shared.Entities.Unit.Gram }]
        };
    }
}
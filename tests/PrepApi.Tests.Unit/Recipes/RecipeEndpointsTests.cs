using Microsoft.AspNetCore.Http.HttpResults;

using PrepApi.Recipes;
using PrepApi.Recipes.Requests;
using PrepApi.Shared.Services;
using PrepApi.Tests.Integration.Helpers;
using PrepApi.Tests.Unit.Helpers;

namespace PrepApi.Tests.Unit.Recipes;

public class RecipeEndpointsTests
{
    private readonly IUserContext _userContext;
    private readonly FakeDb _fakeDb;

    public RecipeEndpointsTests()
    {
        _userContext = TestUserContext.Authenticated();
        _fakeDb = new FakeDb(_userContext);
    }

    [Fact]
    public async Task CreateVariantFromPrep_PrepNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var request = new CreateVariantFromPrepRequest { Name = "Variant", SetAsFavorite = false };

        // Act
        var result = await RecipeEndpoints.CreateVariantFromPrep(Guid.NewGuid(), request, context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task CreateVariantFromPrep_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await context.SeedRecipeAsync();
        var prep = await context.SeedPrepAsync(recipe);
        var request = new CreateVariantFromPrepRequest { Name = "Variant", SetAsFavorite = true };

        // Act
        var result = await RecipeEndpoints.CreateVariantFromPrep(prep.Id, request, context, _userContext);

        // Assert
        Assert.IsType<Created<Guid>>(result.Result);
    }

    [Fact]
    public async Task SetFavoriteVariant_VariantNotFound_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();

        // Act
        var result = await RecipeEndpoints.SetFavoriteVariant(Guid.NewGuid(), context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task SetFavoriteVariant_ValidRequest_UpdatesFavorite()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var recipe = await context.SeedRecipeAsync();
        var variant1 = await context.SeedVariantRecipeAsync(recipe.Id, "Variant 1", true);
        var variant2 = await context.SeedVariantRecipeAsync(recipe.Id, "Variant 2", false);

        // Act
        var result = await RecipeEndpoints.SetFavoriteVariant(variant2.Id, context, _userContext);

        // Assert
        Assert.IsType<NoContent>(result.Result);
        var updated1 = await context.Recipes.FindAsync(variant1.Id);
        var updated2 = await context.Recipes.FindAsync(variant2.Id);
        Assert.NotNull(updated1);
        Assert.NotNull(updated2);
        Assert.False(updated1.IsFavoriteVariant);
        Assert.True(updated2.IsFavoriteVariant);
    }
}
using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Recipes.Requests;
using PrepApi.Tests.Integration.TestHelpers;

namespace PrepApi.Tests.Integration;

public class RecipeVariantBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserCreatesVariantFromPrep()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var recipe = await context.SeedRecipeAsync();
        var prep = await context.SeedPrepAsync(recipe);
        var request = new CreateVariantFromPrepRequest
        {
            Name = "Test Variant",
            SetAsFavorite = true
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/recipes/{prep.Id}/variants", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var variantId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, variantId);

        await using var assertContext = await factory.CreateScopedDbContextAsync();
        var variant = await assertContext.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == variantId);

        Assert.NotNull(variant);
        Assert.Equal(request.Name, variant.Name);
        Assert.Equal(recipe.Id, variant.OriginalRecipeId);
        Assert.True(variant.IsFavoriteVariant);
        Assert.Equal(prep.PrepIngredients.Count, variant.RecipeIngredients.Count);
    }

    [Fact]
    public async Task UserSetsFavoriteVariant()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var originalRecipe = await context.SeedRecipeAsync();
        var variant1 = await context.SeedRecipeAsync(
            name: "Variant 1",
            originalRecipeId: originalRecipe.Id,
            isFavoriteVariant: true);
        var variant2 = await context.SeedRecipeAsync(
            name: "Variant 2",
            originalRecipeId: originalRecipe.Id,
            isFavoriteVariant: false);

        // Act
        var response = await _client.PutAsync($"/api/recipes/{variant2.Id}/favorite", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var assertContext = await factory.CreateScopedDbContextAsync();
        var updatedVariant1 = await assertContext.Recipes.FindAsync(variant1.Id);
        var updatedVariant2 = await assertContext.Recipes.FindAsync(variant2.Id);

        Assert.NotNull(updatedVariant1);
        Assert.NotNull(updatedVariant2);
        Assert.False(updatedVariant1.IsFavoriteVariant);
        Assert.True(updatedVariant2.IsFavoriteVariant);
    }
}
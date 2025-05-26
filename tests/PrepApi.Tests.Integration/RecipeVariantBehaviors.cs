using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PrepApi.Contracts;
using PrepApi.Data;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration;

public class RecipeVariantBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly TestSeeder _seeder = new(factory);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserCreatesVariantFromPrep()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync();
        var prep = await _seeder.SeedPrepAsync(recipe);
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

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();

        var variant = await dbContext.Recipes
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
        var originalRecipe = await _seeder.SeedRecipeAsync();
        var variant1 = await _seeder.SeedRecipeAsync(
            name: "Variant 1",
            originalRecipeId: originalRecipe.Id,
            isFavoriteVariant: true);
        var variant2 = await _seeder.SeedRecipeAsync(
            name: "Variant 2",
            originalRecipeId: originalRecipe.Id,
            isFavoriteVariant: false);

        // Act
        var response = await _client.PutAsync($"/api/recipes/{variant2.Id}/favorite", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();

        var updatedVariant1 = await dbContext.Recipes.FindAsync(variant1.Id);
        var updatedVariant2 = await dbContext.Recipes.FindAsync(variant2.Id);

        Assert.NotNull(updatedVariant1);
        Assert.NotNull(updatedVariant2);
        Assert.False(updatedVariant1.IsFavoriteVariant);
        Assert.True(updatedVariant2.IsFavoriteVariant);
    }
}
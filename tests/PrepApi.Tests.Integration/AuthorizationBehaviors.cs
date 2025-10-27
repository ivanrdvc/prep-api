using System.Net;
using System.Net.Http.Json;

using PrepApi.Data;
using PrepApi.Tests.Integration.TestHelpers;

namespace PrepApi.Tests.Integration;

public class AuthorizationBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    [Fact]
    public async Task UserCannotGetCurrentUserWhenNotAuthenticated()
    {
        // Arrange
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotGetCurrentUserWhenNotRegistered()
    {
        // Arrange
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.AuthenticationHeaderName);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticationHeaderName, "non-existent-user-id");

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserGetsCurrentUser()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdatePrepOwnedByAnotherUser()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var otherUserId = Guid.NewGuid();
        await context.SeedUserAsync(
            userId: otherUserId,
            externalId: "other-user-prep-update",
            email: "other-prep-update@example.com");

        var ingredients = await context.SeedIngredientsAsync("Flour");
        var recipeForOtherUser = await context.SeedRecipeAsync(
            userId: otherUserId,
            ingredients: [(ingredients["Flour"], 100, Unit.Gram)]);
        var prepForOtherUser = await context.SeedPrepAsync(recipeForOtherUser);

        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync($"/api/preps/{prepForOtherUser.Id}", new
        {
            recipeId = recipeForOtherUser.Id,
            summaryNotes = "Hacked prep!",
            prepTimeMinutes = 10,
            cookTimeMinutes = 20,
            steps = new[] { new { order = 1, description = "Malicious step" } },
            prepIngredients = new[] { new { ingredientId = ingredients["Flour"].Id, quantity = 100, unit = 2, notes = "" } }
        });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdatePrepRatingOwnedByAnotherUser()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var otherUserId = Guid.NewGuid();
        await context.SeedUserAsync(
            userId: otherUserId,
            externalId: "other-user-rating",
            email: "other-rating@example.com");

        var recipeForOtherUser = await context.SeedRecipeAsync(userId: otherUserId);
        var prepForOtherUser = await context.SeedPrepAsync(recipeForOtherUser);
        var ratingForOtherUser = await context.SeedPrepRatingAsync(
            prepId: prepForOtherUser.Id,
            userId: otherUserId);

        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/preps/{prepForOtherUser.Id}/ratings/{ratingForOtherUser.Id}",
            new
            {
                liked = false,
                overallRating = 1,
                dimensions = new Dictionary<string, int> { { "taste", 1 } },
                whatWorkedWell = "Hacked rating!",
                whatToChange = "Everything",
                additionalNotes = "Malicious"
            });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdateIngredientOwnedByAnotherUser()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var otherUserId = Guid.NewGuid();
        await context.SeedUserAsync(
            userId: otherUserId,
            externalId: "other-user-ingredient",
            email: "other-ingredient@example.com");

        var ingredientForOtherUser = new Ingredients.Ingredient
        {
            Name = "Private Ingredient",
            UserId = otherUserId,
            Category = "Other User's Category"
        };
        context.Ingredients.Add(ingredientForOtherUser);
        await context.SaveChangesAsync();

        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/ingredients/{ingredientForOtherUser.Id}",
            new
            {
                name = "Hacked Ingredient",
                category = "Malicious"
            });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdateSharedIngredient()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var sharedIngredient = new Ingredients.Ingredient
        {
            Name = "Shared Flour",
            UserId = null, // Shared ingredient
            Category = "Grains"
        };
        context.Ingredients.Add(sharedIngredient);
        await context.SaveChangesAsync();

        var client = factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/ingredients/{sharedIngredient.Id}",
            new
            {
                name = "Hacked Shared Flour",
                category = "Malicious"
            });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
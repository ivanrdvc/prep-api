using System.Net;
using System.Net.Http.Json;

using PrepApi.Data;
using PrepApi.Preps.Requests;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration;

public class PrepRatingBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private Dictionary<string, Ingredient> _ingredients = new();

    public async Task InitializeAsync()
    {
        await using var context = await factory.CreateScopedDbContextAsync();
        await context.SeedDefaultRatingDimensionsAsync();
        _ingredients = await context.SeedIngredientsAsync("Flour", "Sugar", "Milk", "Eggs");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserRatesPrep()
    {
        // Arrange
        await using var context = await factory.CreateScopedDbContextAsync();
        var recipe = await context.SeedRecipeAsync(ingredients: [(_ingredients["Sugar"], 50, Unit.Gram)]);
        var prep = await context.SeedPrepAsync(recipe, summaryNotes: "Prep to rate");
        var ratingRequest = new UpsertPrepRatingRequest
        {
            OverallRating = 4,
            Liked = true,
            Dimensions = new Dictionary<string, int> { { "taste", 4 }, { "texture", 5 }, { "appearance", 4 } },
            WhatWorkedWell = "Easy to follow",
            WhatToChange = "Nothing",
            AdditionalNotes = "Great prep!"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/preps/{prep.Id}/ratings", ratingRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdRatingId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, createdRatingId);
    }
}
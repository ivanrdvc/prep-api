using System.Net;
using System.Net.Http.Json;

using PrepApi.Preps.Requests;
using PrepApi.Shared.Entities;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration;

public class PrepRatingBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly TestSeeder _seeder = new(factory);
    private Dictionary<string, Ingredient> _ingredients = new();

    public async Task InitializeAsync()
    {
        await _seeder.SeedDefaultRatingDimensionsAsync();
        _ingredients = await _seeder.SeedIngredientsAsync("Flour", "Sugar", "Milk", "Eggs");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserRatesCompletedPrep()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync(
            name: "Recipe for Rating",
            ingredients: [(_ingredients["Sugar"], 50, Unit.Gram)]
        );
        var prep = await _seeder.SeedPrepAsync(recipe, summaryNotes: "Prep to rate");
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
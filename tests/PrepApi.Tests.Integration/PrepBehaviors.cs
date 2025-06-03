using System.Net;
using System.Net.Http.Json;

using PrepApi.Preps;
using PrepApi.Preps.Entities;
using PrepApi.Preps.Requests;
using PrepApi.Shared.Dtos;
using PrepApi.Shared.Entities;
using PrepApi.Shared.Requests;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration;

public class PrepBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly TestSeeder _seeder = new(factory);

    private Dictionary<string, Ingredient> _ingredients = new();

    public async Task InitializeAsync()
    {
        _ingredients = await _seeder.SeedIngredientsAsync("Flour", "Sugar", "Milk", "Eggs");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserCreatesPrep()
    {
        // Arrange
        var baseRecipe = await _seeder.SeedRecipeAsync(
            name: "Simple Recipe",
            ingredients: [(_ingredients["Flour"], 100, Unit.Gram)]
        );

        var createRequest = new UpsertPrepRequest
        {
            RecipeId = baseRecipe.Id,
            SummaryNotes = "Test prep notes",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 20,
            Steps = [new() { Order = 1, Description = "Test prep step" }],
            PrepIngredients =
            [
                new()
                {
                    IngredientId = _ingredients["Flour"].Id, Quantity = 150, Unit = Unit.Gram,
                    Notes = "Increased amount"
                },
                new() { IngredientId = _ingredients["Sugar"].Id, Quantity = 50, Unit = Unit.Gram },
                new() { IngredientId = _ingredients["Milk"].Id, Quantity = 100, Unit = Unit.Milliliter }
            ]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/preps", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdPrepId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, createdPrepId);
    }

    [Fact]
    public async Task UserViewsOwnPrepDetails()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync(
            name: "Test Recipe",
            ingredients: [(_ingredients["Eggs"], 2, Unit.Whole)]
        );

        var seededPrep = await _seeder.SeedPrepAsync(
            recipe,
            summaryNotes: "Test prep notes",
            steps:
            [
                new() { Order = 1, Description = "Test step 1" },
                new() { Order = 2, Description = "Test step 2" }
            ],
            ingredients:
            [
                (_ingredients["Eggs"], 3, Unit.Whole, "Used more", PrepIngredientStatus.Modified),
                (_ingredients["Milk"], 100, Unit.Milliliter, null, PrepIngredientStatus.Added)
            ]
        );

        // Act
        var response = await _client.GetAsync($"/api/preps/{seededPrep.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var prepDto = await response.Content.ReadFromJsonAsync<PrepDto>();
        Assert.NotNull(prepDto);
    }

    [Fact]
    public async Task UserDeletesOwnPrep()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync(ingredients: [(_ingredients["Flour"], 100, Unit.Gram)]);

        var seededPrep = await _seeder.SeedPrepAsync(
            recipe,
            summaryNotes: "Test prep to delete"
        );
        var prepIdToDelete = seededPrep.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/preps/{prepIdToDelete}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UserViewsPaginatedPrepsForRecipe()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync(
            name: "Recipe with Multiple Preps",
            ingredients: [(_ingredients["Flour"], 100, Unit.Gram)]
        );

        await _seeder.SeedPrepAsync(recipe, summaryNotes: "First prep notes");
        await _seeder.SeedPrepAsync(recipe, summaryNotes: "Second prep notes");
        await _seeder.SeedPrepAsync(recipe, summaryNotes: "Third prep notes");

        // Act
        var response = await _client.GetAsync($"/api/preps/recipe/{recipe.Id}?pageSize=2&pageIndex=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paginatedResult = await response.Content.ReadFromJsonAsync<PaginatedItems<PrepSummaryDto>>();
        Assert.NotNull(paginatedResult);

        // Should return 2 items (PageSize=2) with total count of 3
        Assert.Equal(2, paginatedResult.Data.Count());
        Assert.Equal(3, paginatedResult.Count);

        var page2Response = await _client.GetAsync($"/api/preps/recipe/{recipe.Id}?pageSize=2&pageIndex=1");
        Assert.Equal(HttpStatusCode.OK, page2Response.StatusCode);

        var page2Result = await page2Response.Content.ReadFromJsonAsync<PaginatedItems<PrepSummaryDto>>();
        Assert.NotNull(page2Result);

        // Should return 1 item (the third prep) with total count of 3
        Assert.Single(page2Result.Data);
        Assert.Equal(3, page2Result.Count);
    }

    [Fact]
    public async Task UserUpdatesOwnPrep()
    {
        // Arrange
        var baseRecipe = await _seeder.SeedRecipeAsync(
            name: "Recipe to Update",
            ingredients:
            [
                (_ingredients["Flour"], 100, Unit.Gram),
                (_ingredients["Sugar"], 50, Unit.Gram)
            ]
        );

        var originalPrep = await _seeder.SeedPrepAsync(
            baseRecipe,
            summaryNotes: "Original prep notes",
            prepTimeMinutes: 15,
            cookTimeMinutes: 30,
            steps:
            [
                new() { Order = 1, Description = "Original step 1" },
                new() { Order = 2, Description = "Original step 2" }
            ],
            ingredients:
            [
                (_ingredients["Flour"], 100, Unit.Gram, null, PrepIngredientStatus.Kept),
                (_ingredients["Sugar"], 50, Unit.Gram, null, PrepIngredientStatus.Kept)
            ]
        );

        var updateRequest = new UpsertPrepRequest
        {
            RecipeId = baseRecipe.Id,
            SummaryNotes = "Updated prep notes",
            PrepTimeMinutes = 20,
            CookTimeMinutes = 35,
            Steps = new List<StepDto>
            {
                new() { Order = 1, Description = "Updated step 1" },
                new() { Order = 2, Description = "Updated step 2" },
                new() { Order = 3, Description = "New step 3" }
            },
            PrepIngredients = new List<PrepIngredientInputDto>
            {
                new()
                {
                    IngredientId = _ingredients["Flour"].Id, Quantity = 150, Unit = Unit.Gram,
                    Notes = "Increased amount"
                },
                new()
                {
                    IngredientId = _ingredients["Milk"].Id, Quantity = 200, Unit = Unit.Milliliter, Notes = "Added milk"
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/preps/{originalPrep.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the prep was updated correctly
        var getResponse = await _client.GetAsync($"/api/preps/{originalPrep.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updatedPrepDto = await getResponse.Content.ReadFromJsonAsync<PrepDto>();
        Assert.NotNull(updatedPrepDto);
        Assert.Equal("Updated prep notes", updatedPrepDto.SummaryNotes);
        Assert.Equal(20, updatedPrepDto.PrepTimeMinutes);
        Assert.Equal(35, updatedPrepDto.CookTimeMinutes);
        Assert.Equal(3, updatedPrepDto.Steps.Count);
        Assert.Equal("Updated step 1", updatedPrepDto.Steps[0].Description);
        Assert.Equal("New step 3", updatedPrepDto.Steps[2].Description);

        // Verify ingredients with their statuses
        Assert.Equal(2, updatedPrepDto.PrepIngredients.Count);

        var flourIngredient = updatedPrepDto.PrepIngredients.First(i => i.IngredientId == _ingredients["Flour"].Id);
        Assert.Equal(150, flourIngredient.Quantity);
        Assert.Equal(Unit.Gram, flourIngredient.Unit);
        Assert.Equal("Increased amount", flourIngredient.Notes);
        Assert.Equal(PrepIngredientStatus.Modified, flourIngredient.Status);

        var milkIngredient = updatedPrepDto.PrepIngredients.First(i => i.IngredientId == _ingredients["Milk"].Id);
        Assert.Equal(200, milkIngredient.Quantity);
        Assert.Equal(Unit.Milliliter, milkIngredient.Unit);
        Assert.Equal("Added milk", milkIngredient.Notes);
        Assert.Equal(PrepIngredientStatus.Added, milkIngredient.Status);
    }
}
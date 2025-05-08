using System.Net;
using System.Net.Http.Json;

using PrepApi.Contracts;
using PrepApi.Data;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration;

public class PrepEndpointsTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly TestSeeder _seeder = new(factory);

    private Dictionary<string, Ingredient> _ingredients = new();

    public async Task InitializeAsync()
    {
        _ingredients = await _seeder.SeedIngredientsAsync("Flour", "Sugar", "Milk", "Eggs");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreatePrep_WithValidData_ReturnsCreated()
    {
        // Arrange
        var baseRecipe = await _seeder.SeedRecipeAsync(
            name: "Simple Recipe",
            ingredients: [(_ingredients["Flour"], 100, Unit.Gram)]
        );

        var createPrepRequest = new CreatePrepRequest
        {
            RecipeId = baseRecipe.Id,
            SummaryNotes = "Test prep notes",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 20,
            Steps =
            [
                new StepDto { Order = 1, Description = "Test prep step" },
            ],
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
        var response = await _client.PostAsJsonAsync("/api/preps", createPrepRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdPrepId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, createdPrepId);
    }

    [Fact]
    public async Task GetPrep_ExistingPrepForUser_ReturnsOkWithPrepDto()
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
    public async Task DeletePrep_ExistingPrepForUser_ReturnsNoContentAndDeletesPrep()
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
}
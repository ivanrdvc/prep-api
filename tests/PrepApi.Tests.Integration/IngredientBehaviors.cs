using System.Net;
using System.Net.Http.Json;

using PrepApi.Ingredients;
using PrepApi.Ingredients.Requests;
using PrepApi.Tests.Integration.TestHelpers;

namespace PrepApi.Tests.Integration;

public class IngredientBehaviors(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await using var context = await factory.CreateScopedDbContextAsync();
        await context.SeedIngredientsAsync("Flour", "Sugar", "Salt");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserCreatesCustomIngredient()
    {
        // Arrange
        var request = new UpsertIngredientRequest { Name = "Custom Spice" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingredients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ingredient = await response.Content.ReadFromJsonAsync<IngredientDto>();
        Assert.NotNull(ingredient);
        Assert.Equal("Custom Spice", ingredient.Name);
        Assert.False(ingredient.IsShared);
    }

    [Fact]
    public async Task UserCannotCreateIngredientWithEmptyName()
    {
        // Arrange
        var request = new UpsertIngredientRequest { Name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingredients", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotCreateDuplicateIngredient()
    {
        // Arrange
        var request = new UpsertIngredientRequest { Name = "Duplicate Item" };
        await _client.PostAsJsonAsync("/api/ingredients", request);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ingredients", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UserViewsSharedAndOwnIngredients()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/ingredients", new UpsertIngredientRequest { Name = "My Ingredient" });

        // Act
        var response = await _client.GetAsync("/api/ingredients?skip=0&take=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ingredients = await response.Content.ReadFromJsonAsync<List<IngredientDto>>();
        Assert.NotNull(ingredients);
        Assert.Contains(ingredients, i => i is { Name: "Flour", IsShared: true });
        Assert.Contains(ingredients, i => i is { Name: "My Ingredient", IsShared: false });
    }

    [Fact]
    public async Task UserFiltersIngredientsByName()
    {
        // Act
        var response = await _client.GetAsync("/api/ingredients?skip=0&take=10&name=Sug");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ingredients = await response.Content.ReadFromJsonAsync<List<IngredientDto>>();
        Assert.NotNull(ingredients);
        Assert.All(ingredients, i => Assert.Contains("Sug", i.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UserSearchesIngredients()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/ingredients", new UpsertIngredientRequest { Name = "Paprika" });

        // Act
        var response = await _client.GetAsync("/api/ingredients/search?query=pap");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<IngredientDto>>();
        Assert.NotNull(results);
        Assert.Contains(results, i => i.Name.Contains("Paprika", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UserCannotSearchWithEmptyQuery()
    {
        // Act
        var response = await _client.GetAsync("/api/ingredients/search?query=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotAccessIngredientsWhenNotAuthenticated()
    {
        // Arrange
        var unauthenticatedClient = factory.CreateUnauthenticatedClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/ingredients?skip=0&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserUpdatesOwnIngredient()
    {
        // Arrange
        var createRequest = new UpsertIngredientRequest { Name = "Original Ingredient" };
        var createResponse = await _client.PostAsJsonAsync("/api/ingredients", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientDto>();
        Assert.NotNull(created);

        var updateRequest = new UpsertIngredientRequest { Name = "Updated Ingredient", Category = "Vegetables" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/ingredients/{created.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdateNonExistentIngredient()
    {
        // Arrange
        var request = new UpsertIngredientRequest { Name = "Test" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/ingredients/{Guid.NewGuid()}", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserCannotUpdateIngredientWithDuplicateName()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/ingredients", new UpsertIngredientRequest { Name = "First Item" });
        var secondResponse = await _client.PostAsJsonAsync("/api/ingredients", new UpsertIngredientRequest { Name = "Second Item" });
        var second = await secondResponse.Content.ReadFromJsonAsync<IngredientDto>();
        Assert.NotNull(second);

        var updateRequest = new UpsertIngredientRequest { Name = "First Item" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/ingredients/{second.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UserDeletesOwnIngredient()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/ingredients", new UpsertIngredientRequest { Name = "To Delete" });
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientDto>();
        Assert.NotNull(created);

        // Act
        var response = await _client.DeleteAsync($"/api/ingredients/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's deleted
        var getResponse = await _client.GetAsync("/api/ingredients?skip=0&take=10");
        var ingredients = await getResponse.Content.ReadFromJsonAsync<List<IngredientDto>>();
        Assert.DoesNotContain(ingredients!, i => i.Id == created.Id);
    }

    [Fact]
    public async Task UserCannotDeleteNonExistentIngredient()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/ingredients/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
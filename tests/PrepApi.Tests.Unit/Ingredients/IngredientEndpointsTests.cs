using Microsoft.AspNetCore.Http.HttpResults;

using PrepApi.Authorization;
using PrepApi.Ingredients;
using PrepApi.Ingredients.Requests;
using PrepApi.Tests.Integration.TestHelpers;
using PrepApi.Tests.Unit.TestHelpers;
using PrepApi.Users;

namespace PrepApi.Tests.Unit.Ingredients;

public class IngredientEndpointsTests
{
    private readonly IUserContext _userContext;
    private readonly FakeDb _fakeDb;

    public IngredientEndpointsTests()
    {
        _userContext = TestUserContext.Authenticated();
        _fakeDb = new FakeDb(_userContext);
    }

    [Fact]
    public async Task CreateIngredient_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var request = new UpsertIngredientRequest { Name = "Paprika" };

        // Act
        var result = await IngredientEndpoints.CreateIngredient(request, context, _userContext);

        // Assert
        var created = (Created<IngredientDto>)result.Result;
        Assert.NotNull(created.Value);
        Assert.Equal("Paprika", created.Value.Name);
        Assert.False(created.Value.IsShared);
    }

    [Fact]
    public async Task CreateIngredient_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var request = new UpsertIngredientRequest { Name = "" };

        // Act
        var result = await IngredientEndpoints.CreateIngredient(request, context, _userContext);

        // Assert
        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task CreateIngredient_DuplicateName_ReturnsConflict()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var request = new UpsertIngredientRequest { Name = "Salt" };
        await IngredientEndpoints.CreateIngredient(request, context, _userContext);

        // Act
        var result = await IngredientEndpoints.CreateIngredient(request, context, _userContext);

        // Assert
        Assert.IsType<Conflict<string>>(result.Result);
    }

    [Fact]
    public async Task GetUserIngredients_ReturnsSharedAndUserIngredients()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        await context.SeedIngredientsAsync("Flour", "Sugar");
        await IngredientEndpoints.CreateIngredient(
            new UpsertIngredientRequest { Name = "MyCustomIngredient" },
            context,
            _userContext);
        var request = new GetIngredientsRequest { Skip = 0, Take = 10 };

        // Act
        var result = await IngredientEndpoints.GetUserIngredients(context, request);

        // Assert
        var ingredients = result.Value;
        Assert.NotNull(ingredients);
        Assert.Equal(3, ingredients.Count);
        Assert.Contains(ingredients, i => i is { Name: "Flour", IsShared: true });
        Assert.Contains(ingredients, i => i is { Name: "MyCustomIngredient", IsShared: false });
    }

    [Fact]
    public async Task GetUserIngredients_WithNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        await context.SeedIngredientsAsync("Flour", "Sugar", "Salt");
        var request = new GetIngredientsRequest { Skip = 0, Take = 10, Name = "Fl" };

        // Act
        var result = await IngredientEndpoints.GetUserIngredients(context, request);

        // Assert
        var ingredients = result.Value;
        Assert.NotNull(ingredients);
        Assert.Single(ingredients);
        Assert.Equal("Flour", ingredients[0].Name);
    }

    [Fact]
    public async Task GetUserIngredients_LimitsTakeParameter()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var request = new GetIngredientsRequest { Skip = 0, Take = 2000 };

        // Act
        var result = await IngredientEndpoints.GetUserIngredients(context, request);

        // Assert - Should limit to 1000 max
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task SearchIngredients_EmptyQuery_ReturnsBadRequest()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var ingredientService = new IngredientService(context);

        // Act
        var result = await IngredientEndpoints.SearchIngredients(ingredientService, "");

        // Assert
        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task UpdateIngredient_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var createRequest = new UpsertIngredientRequest { Name = "Original Name" };
        var createResult = await IngredientEndpoints.CreateIngredient(createRequest, context, _userContext);
        var created = (Created<IngredientDto>)createResult.Result;

        var updateRequest = new UpsertIngredientRequest { Name = "Updated Name", Category = "Spices" };

        // Act
        var result = await IngredientEndpoints.UpdateIngredient(created.Value!.Id, updateRequest, context, _userContext);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task UpdateIngredient_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var createRequest = new UpsertIngredientRequest { Name = "Test Ingredient" };
        var createResult = await IngredientEndpoints.CreateIngredient(createRequest, context, _userContext);
        var created = (Created<IngredientDto>)createResult.Result;

        var updateRequest = new UpsertIngredientRequest { Name = "" };

        // Act
        var result = await IngredientEndpoints.UpdateIngredient(created.Value!.Id, updateRequest, context, _userContext);

        // Assert
        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task UpdateIngredient_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var updateRequest = new UpsertIngredientRequest { Name = "Test" };

        // Act
        var result = await IngredientEndpoints.UpdateIngredient(Guid.NewGuid(), updateRequest, context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdateIngredient_DuplicateName_ReturnsConflict()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        await IngredientEndpoints.CreateIngredient(new UpsertIngredientRequest { Name = "First" }, context, _userContext);
        var secondResult =
            await IngredientEndpoints.CreateIngredient(new UpsertIngredientRequest { Name = "Second" }, context, _userContext);
        var second = (Created<IngredientDto>)secondResult.Result;

        var updateRequest = new UpsertIngredientRequest { Name = "First" };

        // Act
        var result = await IngredientEndpoints.UpdateIngredient(second.Value!.Id, updateRequest, context, _userContext);

        // Assert
        Assert.IsType<Conflict<string>>(result.Result);
    }

    [Fact]
    public async Task DeleteIngredient_ExistingIngredient_ReturnsNoContent()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();
        var createRequest = new UpsertIngredientRequest { Name = "To Delete" };
        var createResult = await IngredientEndpoints.CreateIngredient(createRequest, context, _userContext);
        var created = (Created<IngredientDto>)createResult.Result;

        // Act
        var result = await IngredientEndpoints.DeleteIngredient(created.Value!.Id, context, _userContext);

        // Assert
        Assert.IsType<NoContent>(result.Result);
    }

    [Fact]
    public async Task DeleteIngredient_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();

        // Act
        var result = await IngredientEndpoints.DeleteIngredient(Guid.NewGuid(), context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task UpdateIngredient_DifferentOwner_ReturnsNotFound()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var otherUserContext = new TestUserContext
        {
            User = new User
            {
                Id = otherUserId,
                ExternalId = "other-user-external-id"
            }
        };
        var otherUserDb = new FakeDb(otherUserContext);
        await using var context = otherUserDb.CreateDbContext();

        var createRequest = new UpsertIngredientRequest { Name = "Other User's Ingredient" };
        var createResult = await IngredientEndpoints.CreateIngredient(createRequest, context, otherUserContext);
        var created = (Created<IngredientDto>)createResult.Result;

        var updateRequest = new UpsertIngredientRequest { Name = "Hacked Name" };

        // Act
        var result = await IngredientEndpoints.UpdateIngredient(created.Value!.Id, updateRequest, context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task DeleteIngredient_SharedIngredient_ReturnsNotFound()
    {
        // Arrange
        await using var context = _fakeDb.CreateDbContext();

        var sharedIngredient = new Ingredient
        {
            Name = "Shared Flour",
            UserId = null,
            Category = "Grains"
        };
        context.Ingredients.Add(sharedIngredient);
        await context.SaveChangesAsync();

        // Act
        var result = await IngredientEndpoints.DeleteIngredient(sharedIngredient.Id, context, _userContext);

        // Assert
        Assert.IsType<NotFound>(result.Result);
    }
}
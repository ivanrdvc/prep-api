﻿using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PrepApi.Contracts;
using PrepApi.Data;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration.Endpoints;

public class RecipeEndpointsTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private const string TestUserId = TestAuthenticationHandler.TestUserId;
    private readonly TestSeeder _seeder = new(factory);

    private Dictionary<string, Ingredient> _ingredients = new();
    private Dictionary<string, Tag> _tags = new();

    public async Task InitializeAsync()
    {
        _ingredients = await _seeder.SeedIngredientsAsync("Flour", "Sugar", "Milk", "Butter");
        _tags = await _seeder.SeedTagsAsync(TestUserId, "Tag1", "Test Tag");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetRecipe_WhenRecipeExistsAndBelongsToUser_ReturnsOk()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync(
            ingredients: [(_ingredients["Flour"], 100, Unit.Gram)],
            tags: [_tags["Tag1"], _tags["Test Tag"]]);

        // Act
        var response = await _client.GetAsync($"/api/recipes/{recipe.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returnedRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
        Assert.NotNull(returnedRecipe);
        Assert.Equal(recipe.Id, returnedRecipe.Id);
        Assert.Equal(recipe.Name, returnedRecipe.Name);
        Assert.Equal(recipe.Description, returnedRecipe.Description);
        Assert.Equal(recipe.PrepTimeMinutes, returnedRecipe.PrepTimeMinutes);
        Assert.Equal(recipe.CookTimeMinutes, returnedRecipe.CookTimeMinutes);
        Assert.Equal(recipe.Yield, returnedRecipe.Yield);
        Assert.NotNull(returnedRecipe.Tags);
    }

    [Fact]
    public async Task GetRecipe_WhenRecipeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/recipes/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecipe_WhenRecipeExistsButBelongsToDifferentUser_ReturnsNotFound()
    {
        // Arrange
        const string otherUserId = "other-user-id-abc";
        var recipeForOtherUser = await _seeder.SeedRecipeAsync(userId: otherUserId);

        // Act
        var response = await _client.GetAsync($"/api/recipes/{recipeForOtherUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecipe_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var recipe = await _seeder.SeedRecipeAsync(
            ingredients: [(_ingredients["Milk"], 100, Unit.Milliliter)]);

        var unauthenticatedClient = factory.CreateUnauthenticatedClient();

        // Act
        var response = await unauthenticatedClient.GetAsync($"/api/recipes/{recipe.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRecipe_ShouldReturnVariantsInfo()
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
        var response = await _client.GetAsync($"/api/recipes/{originalRecipe.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var recipeDto = await response.Content.ReadFromJsonAsync<RecipeDto>();
        Assert.NotNull(recipeDto);
        Assert.Equal(2, recipeDto.Variants.Count);
        Assert.Contains(recipeDto.Variants, v => v.Name == variant1.Name && v.IsFavorite);
        Assert.Contains(recipeDto.Variants, v => v.Name != variant2.Name && !v.IsFavorite);
    }

    [Fact]
    public async Task CreateRecipe_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createRequest = new UpsertRecipeRequest
        {
            Name = "Test Recipe",
            Description = "Test recipe description",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Yield = "4 servings",
            Steps =
            [
                new StepDto { Order = 1, Description = "Test step 1" },
                new StepDto { Order = 2, Description = "Test step 2" }
            ],
            Ingredients =
            [
                new RecipeIngredientInputDto
                    { IngredientId = _ingredients["Flour"].Id, Quantity = 100, Unit = Unit.Gram },
                new RecipeIngredientInputDto
                    { IngredientId = _ingredients["Sugar"].Id, Quantity = 50, Unit = Unit.Gram }
            ],
            TagIds = [_tags["Tag1"].Id, _tags["Test Tag"].Id]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/recipes", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdRecipeId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, createdRecipeId);

        await using var assertScope = factory.Services.CreateAsyncScope();
        var dbContextAssert = assertScope.ServiceProvider.GetRequiredService<PrepDb>();
        var recipeInDb = await dbContextAssert.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
            .FirstOrDefaultAsync(r => r.Id == createdRecipeId);

        Assert.NotNull(recipeInDb);
        Assert.Equal(createRequest.Name, recipeInDb.Name);
        Assert.Equal(TestUserId, recipeInDb.UserId);
        Assert.Equal(createRequest.Description, recipeInDb.Description);
        Assert.Equal(createRequest.PrepTimeMinutes, recipeInDb.PrepTimeMinutes);
        Assert.Equal(createRequest.CookTimeMinutes, recipeInDb.CookTimeMinutes);
        Assert.Equal(createRequest.Yield, recipeInDb.Yield);

        var persistedSteps = JsonSerializer.Deserialize<List<StepDto>>(recipeInDb.StepsJson);
        Assert.NotNull(persistedSteps);
        Assert.Equal(createRequest.Steps.Count, persistedSteps.Count);
        Assert.Equal(createRequest.Steps[0].Order, persistedSteps[0].Order);
        Assert.Equal(createRequest.Steps[0].Description, persistedSteps[0].Description);
        Assert.Equal(createRequest.Steps[1].Order, persistedSteps[1].Order);
        Assert.Equal(createRequest.Steps[1].Description, persistedSteps[1].Description);

        Assert.Equal(createRequest.Ingredients.Count, recipeInDb.RecipeIngredients.Count);
        var flourIngredient = recipeInDb.RecipeIngredients
            .FirstOrDefault(ri => ri.IngredientId == _ingredients["Flour"].Id);
        Assert.NotNull(flourIngredient);
        Assert.Equal(createRequest.Ingredients[0].Quantity, flourIngredient.Quantity);
        Assert.Equal(createRequest.Ingredients[0].Unit, flourIngredient.Unit);

        Assert.NotNull(recipeInDb.RecipeTags);
        Assert.Equal(_tags.Count, recipeInDb.RecipeTags.Count);
        foreach (var (_, tag) in _tags)
        {
            Assert.Contains(recipeInDb.RecipeTags, rt => rt.TagId == tag.Id && rt.Tag.UserId == TestUserId);
        }
    }

    [Fact]
    public async Task CreateRecipe_WithNonExistentIngredientId_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentIngredientId = Guid.NewGuid();

        var createRequest = new UpsertRecipeRequest
        {
            Name = "Test Recipe",
            Description = "Test recipe description",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 15,
            Yield = "2 servings",
            Steps =
            [
                new StepDto { Order = 1, Description = "Test step 1" }
            ],
            Ingredients =
            [
                new RecipeIngredientInputDto
                    { IngredientId = _ingredients["Flour"].Id, Quantity = 50, Unit = Unit.Gram },
                new RecipeIngredientInputDto
                    { IngredientId = nonExistentIngredientId, Quantity = 100, Unit = Unit.Gram }
            ]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/recipes", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.NotEmpty(problemDetails.Errors["Ingredients"]);
    }

    [Fact]
    public async Task DeleteRecipe_WhenRecipeExistsAndBelongsToUser_DeletesRecipe()
    {
        // Arrange
        var recipeToDelete = await _seeder.SeedRecipeAsync();
        var recipeIdToDelete = recipeToDelete.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/recipes/{recipeIdToDelete}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var assertScope = factory.Services.CreateAsyncScope();
        var dbContextAssert = assertScope.ServiceProvider.GetRequiredService<PrepDb>();
        var recipeInDbAfterDelete = await dbContextAssert.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recipeIdToDelete);

        Assert.Null(recipeInDbAfterDelete);

        var recipeIngredientCount = await dbContextAssert
            .RecipeIngredients.CountAsync(ri => ri.RecipeId == recipeIdToDelete);
        Assert.Equal(0, recipeIngredientCount);
    }

    [Fact]
    public async Task UpdateRecipe_WithValidData_ReturnsNoContentAndUpdatesRecipe()
    {
        // Arrange
        var flour = _ingredients["Flour"];
        var sugar = _ingredients["Sugar"];
        var milk = _ingredients["Milk"];

        var steps = new List<StepDto>
        {
            new() { Order = 1, Description = "Test step 1" },
            new() { Order = 2, Description = "Test step 2" }
        };

        var initialRecipe = await _seeder.SeedRecipeAsync(
            name: "Test Recipe",
            description: "Original description",
            userId: TestUserId,
            prepTimeMinutes: 15,
            cookTimeMinutes: 25,
            yield: "4 servings",
            steps: steps,
            ingredients: [(flour, 200, Unit.Gram), (sugar, 100, Unit.Gram)],
            tags: [_tags["Tag1"]]
        );

        var recipeIdToUpdate = initialRecipe.Id;

        var newTags = await _seeder.SeedTagsAsync(TestUserId, "NewTag");
        var newTag = newTags["NewTag"];

        var updateRequest = new UpsertRecipeRequest
        {
            Name = "Updated Test Recipe",
            Description = "Updated description",
            PrepTimeMinutes = 20,
            CookTimeMinutes = 30,
            Yield = "6 servings",
            Steps =
            [
                new StepDto { Order = 1, Description = "Updated step 1" },
                new StepDto { Order = 2, Description = "Updated step 2" },
                new StepDto { Order = 3, Description = "New step 3" }
            ],
            Ingredients =
            [
                new RecipeIngredientInputDto { IngredientId = flour.Id, Quantity = 300, Unit = Unit.Gram },
                new RecipeIngredientInputDto { IngredientId = milk.Id, Quantity = 250, Unit = Unit.Milliliter }
            ],
            TagIds = [newTag.Id]
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/recipes/{recipeIdToUpdate}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var assertScope = factory.Services.CreateAsyncScope();
        var dbContextAssert = assertScope.ServiceProvider.GetRequiredService<PrepDb>();
        var updatedRecipeInDb = await dbContextAssert.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recipeIdToUpdate);

        Assert.NotNull(updatedRecipeInDb);
        Assert.Equal(updateRequest.Name, updatedRecipeInDb.Name);
        Assert.Equal(updateRequest.Description, updatedRecipeInDb.Description);
        Assert.Equal(updateRequest.PrepTimeMinutes, updatedRecipeInDb.PrepTimeMinutes);
        Assert.Equal(updateRequest.CookTimeMinutes, updatedRecipeInDb.CookTimeMinutes);
        Assert.Equal(updateRequest.Yield, updatedRecipeInDb.Yield);
        Assert.Equal(TestUserId, updatedRecipeInDb.UserId);

        var updatedStepsInDb = JsonSerializer.Deserialize<List<StepDto>>(updatedRecipeInDb.StepsJson);
        Assert.NotNull(updatedStepsInDb);
        Assert.Equal(updateRequest.Steps.Count, updatedStepsInDb.Count);
        Assert.Equal(updateRequest.Steps[0].Description, updatedStepsInDb[0].Description);
        Assert.Equal(updateRequest.Steps[1].Description, updatedStepsInDb[1].Description);
        Assert.Equal(updateRequest.Steps[2].Description, updatedStepsInDb[2].Description);
        Assert.Equal(updateRequest.Steps[0].Order, updatedStepsInDb[0].Order);
        Assert.Equal(updateRequest.Steps[1].Order, updatedStepsInDb[1].Order);
        Assert.Equal(updateRequest.Steps[2].Order, updatedStepsInDb[2].Order);

        // Check updated ingredients
        Assert.Equal(updateRequest.Ingredients.Count, updatedRecipeInDb.RecipeIngredients.Count);

        var flourIngredient = updatedRecipeInDb.RecipeIngredients.FirstOrDefault(ri => ri.IngredientId == flour.Id);
        Assert.NotNull(flourIngredient);
        Assert.Equal(updateRequest.Ingredients[0].Quantity, flourIngredient.Quantity);
        Assert.Equal(updateRequest.Ingredients[0].Unit, flourIngredient.Unit);

        var milkIngredient = updatedRecipeInDb.RecipeIngredients.FirstOrDefault(ri => ri.IngredientId == milk.Id);
        Assert.NotNull(milkIngredient);
        Assert.Equal(updateRequest.Ingredients[1].Quantity, milkIngredient.Quantity);
        Assert.Equal(updateRequest.Ingredients[1].Unit, milkIngredient.Unit);

        var sugarIngredient = updatedRecipeInDb.RecipeIngredients.FirstOrDefault(ri => ri.IngredientId == sugar.Id);
        Assert.Null(sugarIngredient);

        Assert.NotNull(updatedRecipeInDb.RecipeTags);
        Assert.Single(updatedRecipeInDb.RecipeTags);
        Assert.Contains(updatedRecipeInDb.RecipeTags, rt => rt.TagId == newTag.Id && rt.Tag.UserId == TestUserId);
        Assert.DoesNotContain(updatedRecipeInDb.RecipeTags, rt => rt.TagId == _tags["Tag1"].Id);
    }
}
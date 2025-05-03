using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PrepApi.Contracts;
using PrepApi.Data;
using PrepApi.Tests.Integration.Helpers;

namespace PrepApi.Tests.Integration;

public class RecipeEndpointsTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private const string TestUserId = TestAuthenticationHandler.TestUserId;

    [Fact]
    public async Task GetRecipe_WhenRecipeExistsAndBelongsToUser_ReturnsOk()
    {
        // Arrange
        var seededIngredients = await TestDataSeeder.SeedIngredientsAsync(factory);
        var seededRecipe = await TestDataSeeder.SeedTestRecipeAsync(factory, seededIngredients);

        // Act
        var response = await _client.GetAsync($"/api/recipes/{seededRecipe.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recipeDto = await response.Content.ReadFromJsonAsync<RecipeDto>();
        Assert.NotNull(recipeDto);
        Assert.Equal(seededRecipe.Id, recipeDto.Id);
        Assert.Equal(seededRecipe.Name, recipeDto.Name);
        Assert.Equal(seededRecipe.Description, recipeDto.Description);
        Assert.Equal(seededRecipe.PrepTime, recipeDto.PrepTime);
        Assert.Equal(seededRecipe.CookTime, recipeDto.CookTime);
        Assert.Equal(seededRecipe.Yield, recipeDto.Yield);
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
        var recipeForOtherUser = await TestDataSeeder.SeedTestRecipeAsync(factory, [], "Test Recipe", otherUserId);

        // Act
        var response = await _client.GetAsync($"/api/recipes/{recipeForOtherUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecipe_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var seededRecipe = await TestDataSeeder.SeedTestRecipeAsync(factory, []);
        var unauthenticatedClient = factory.CreateUnauthenticatedClient();

        // Act
        var response = await unauthenticatedClient.GetAsync($"/api/recipes/{seededRecipe.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipe_WithValidData_ReturnsCreated()
    {
        // Arrange
        var ingredients = await TestDataSeeder.SeedIngredientsAsync(factory, ["Butter", "Sugar"]);
        var createRequest = new CreateRecipeRequest
        {
            Name = "Test Shortbread",
            Description = "Simple buttery cookies.",
            PrepTime = 10, CookTime = 12, Yield = "12 cookies",
            Steps =
            [
                new StepDto { Order = 1, Description = "Cream butter and sugar." },
                new StepDto { Order = 2, Description = "Gradually add flour and mix until combined." }
            ],
            Ingredients =
            [
                new RecipeIngredientInputDto { IngredientId = ingredients[0].Id, Quantity = 100, Unit = Unit.Gram },
                new RecipeIngredientInputDto { IngredientId = ingredients[1].Id, Quantity = 50, Unit = Unit.Gram }
            ]
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
            .FirstOrDefaultAsync(r => r.Id == createdRecipeId);

        Assert.NotNull(recipeInDb);
        Assert.Equal(createRequest.Name, recipeInDb.Name);
        Assert.Equal(TestUserId, recipeInDb.UserId);
        Assert.Equal(createRequest.Description, recipeInDb.Description);
        Assert.Equal(createRequest.PrepTime, recipeInDb.PrepTime);
        Assert.Equal(createRequest.CookTime, recipeInDb.CookTime);
        Assert.Equal(createRequest.Yield, recipeInDb.Yield);

        var persistedSteps = JsonSerializer.Deserialize<List<StepDto>>(recipeInDb.StepsJson);
        Assert.NotNull(persistedSteps);
        Assert.Equal(createRequest.Steps.Count, persistedSteps.Count);
        Assert.Equal(createRequest.Steps[0].Order, persistedSteps[0].Order);
        Assert.Equal(createRequest.Steps[0].Description, persistedSteps[0].Description);
        Assert.Equal(createRequest.Steps[1].Order, persistedSteps[1].Order);
        Assert.Equal(createRequest.Steps[1].Description, persistedSteps[1].Description);

        Assert.Equal(createRequest.Ingredients.Count, recipeInDb.RecipeIngredients.Count);
        var butterIngredient = recipeInDb.RecipeIngredients
            .FirstOrDefault(ri => ri.IngredientId == ingredients[0].Id);
        Assert.NotNull(butterIngredient);
        Assert.Equal(createRequest.Ingredients[0].Quantity, butterIngredient.Quantity);
        Assert.Equal(createRequest.Ingredients[0].Unit, butterIngredient.Unit);
    }

    [Fact]
    public async Task CreateRecipe_WithNonExistentIngredientId_ReturnsBadRequest()
    {
        // Arrange
        var ingredients = await TestDataSeeder.SeedIngredientsAsync(factory, ["Butter"]);
        var nonExistentIngredientId = Guid.NewGuid();

        var createRequest = new CreateRecipeRequest
        {
            Name = "Test Recipe with Invalid Ingredient",
            Description = "This recipe tries to use an ingredient that doesn't exist.",
            PrepTime = 5, CookTime = 15, Yield = "2 servings",
            Steps =
            [
                new StepDto { Order = 1, Description = "Do step 1." }
            ],
            Ingredients =
            [
                new RecipeIngredientInputDto { IngredientId = ingredients[0].Id, Quantity = 50, Unit = Unit.Gram },
                new RecipeIngredientInputDto
                    { IngredientId = nonExistentIngredientId, Quantity = 100, Unit = Unit.Milliliter }
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
    public async Task CreateRecipe_WithInvalidBasicData_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new CreateRecipeRequest
        {
            Name = "", // Invalid Empty Name triggers validation
            Description = "Valid description.",
            PrepTime = -5, // Invalid Negative PrepTime triggers validation
            CookTime = 10,
            Yield = "Some yield",
            Steps = // Missing description in first step
            [
                new StepDto { Order = 1, Description = "" },
                new StepDto { Order = 2, Description = "Valid step." }
            ],
            Ingredients = // Invalid Zero quantity
            [
                new RecipeIngredientInputDto { IngredientId = Guid.NewGuid(), Quantity = 0, Unit = Unit.Gram }
            ]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/recipes", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("Name", problemDetails.Errors.Keys);
        Assert.NotEmpty(problemDetails.Errors["Name"]);

        Assert.Contains("PrepTime", problemDetails.Errors.Keys);
        Assert.NotEmpty(problemDetails.Errors["PrepTime"]);

        Assert.Contains("Steps[0].Description", problemDetails.Errors.Keys);
        Assert.NotEmpty(problemDetails.Errors["Steps[0].Description"]);

        Assert.Contains("Ingredients[0].Quantity", problemDetails.Errors.Keys);
        Assert.NotEmpty(problemDetails.Errors["Ingredients[0].Quantity"]);
    }

    [Fact]
    public async Task DeleteRecipe_WhenRecipeExistsAndBelongsToUser_DeletesRecipe()
    {
        // Arrange
        var recipeToDelete = await TestDataSeeder.SeedTestRecipeAsync(factory, []);
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
        var initialIngredients = await TestDataSeeder.SeedIngredientsAsync(factory, ["Flour", "Sugar", "Milk"]);
        var flour = initialIngredients.First(i => i.Name == "Flour");
        var sugar = initialIngredients.First(i => i.Name == "Sugar");
        var milk = initialIngredients.First(i => i.Name == "Milk");

        var initialRecipe = await TestDataSeeder.SeedTestRecipeAsync(factory,
            [flour, sugar],
            "Original Cake",
            TestUserId,
            "A simple cake.",
            15, 25, "8 servings",
            [
                new StepDto { Order = 1, Description = "Mix dry ingredients." },
                new StepDto { Order = 2, Description = "Add wet ingredients." }
            ]);

        var recipeIdToUpdate = initialRecipe.Id;

        var updateRequest = new UpdateRecipeRequest
        {
            Name = "Updated Delicious Cake",
            Description = "An improved, more delicious cake recipe.",
            PrepTime = 20,
            CookTime = 30,
            Yield = "10 servings",
            Steps =
            [
                new StepDto { Order = 1, Description = "Combine flour and sugar." },
                new StepDto { Order = 2, Description = "Whisk in milk and eggs." },
                new StepDto { Order = 3, Description = "Bake until golden." }
            ],
            Ingredients =
            [
                new RecipeIngredientInputDto { IngredientId = flour.Id, Quantity = 300, Unit = Unit.Gram },
                new RecipeIngredientInputDto { IngredientId = milk.Id, Quantity = 250, Unit = Unit.Milliliter }
            ]
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/recipes/{recipeIdToUpdate}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        await using var assertScope = factory.Services.CreateAsyncScope();
        var dbContextAssert = assertScope.ServiceProvider.GetRequiredService<PrepDb>();
        var updatedRecipeInDb = await dbContextAssert.Recipes
            .Include(r => r.RecipeIngredients)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recipeIdToUpdate);

        Assert.NotNull(updatedRecipeInDb);
        Assert.Equal(updateRequest.Name, updatedRecipeInDb.Name);
        Assert.Equal(updateRequest.Description, updatedRecipeInDb.Description);
        Assert.Equal(updateRequest.PrepTime, updatedRecipeInDb.PrepTime);
        Assert.Equal(updateRequest.CookTime, updatedRecipeInDb.CookTime);
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
    }
}
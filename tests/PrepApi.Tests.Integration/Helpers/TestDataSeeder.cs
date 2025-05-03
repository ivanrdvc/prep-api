using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Tests.Integration.Helpers;

public static class TestDataSeeder
{
    public static async Task<Recipe> SeedTestRecipeAsync(
        TestWebAppFactory factory,
        IEnumerable<Ingredient> ingredients,
        string recipeName = "Test Recipe",
        string userId = TestAuthenticationHandler.TestUserId,
        string? description = null,
        int? prepTime = null,
        int? cookTime = null,
        string? yield = null,
        List<StepDto>? steps = null,
        double defaultIngredientQuantity = 1,
        Unit defaultIngredientUnit = Unit.Gram
    )
    {
        var stepsToSerialize = steps?.Count > 0
            ? steps
            :
            [
                new() { Order = 1, Description = $"Prepare {recipeName} using provided ingredients." },
                new() { Order = 2, Description = "Cook/assemble as needed." }
            ];

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = recipeName,
            UserId = userId,
            Description = description ?? $"A test recipe for {recipeName}.",
            PrepTime = prepTime ?? 10,
            CookTime = cookTime ?? 20,
            Yield = yield ?? "4 servings",
            StepsJson = JsonSerializer.Serialize(stepsToSerialize),
            CreatedBy = userId,
        };

        foreach (var ingredient in ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Recipe = recipe,
                IngredientId = ingredient.Id,
                Quantity = defaultIngredientQuantity,
                Unit = defaultIngredientUnit
            });
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        await dbContext.Recipes.AddAsync(recipe);
        await dbContext.SaveChangesAsync();

        return recipe;
    }

    public static async Task<List<Ingredient>> SeedIngredientsAsync(
        TestWebAppFactory factory,
        IEnumerable<string>? ingredientNames = null)
    {
        var finalIngredientNames = ingredientNames?.ToList();
        if (finalIngredientNames == null || finalIngredientNames.Count == 0)
        {
            finalIngredientNames = ["Default Seeded Ingredient 1", "Default Seeded Ingredient 2"];
        }

        var ingredientsToSeed = finalIngredientNames
            .Select(name => new Ingredient { Name = name, UserId = null, CreatedBy = "system" })
            .ToList();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        await dbContext.Ingredients.AddRangeAsync(ingredientsToSeed);
        await dbContext.SaveChangesAsync();

        return ingredientsToSeed;
    }
}
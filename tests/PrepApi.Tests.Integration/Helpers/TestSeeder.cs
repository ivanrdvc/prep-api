using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Tests.Integration.Helpers;

public class TestSeeder(TestWebAppFactory factory)
{
    public async Task<Dictionary<string, Ingredient>> SeedIngredientsAsync(params string[] names)
    {
        var ingredients = names.Select(name => new Ingredient
        {
            Name = name,
            UserId = null,
            CreatedBy = "system"
        }).ToList();

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        await dbContext.Ingredients.AddRangeAsync(ingredients);
        await dbContext.SaveChangesAsync();

        return ingredients.ToDictionary(i => i.Name, i => i);
    }

    public async Task<Recipe> SeedRecipeAsync(
        string name = "Test Recipe",
        string? description = null,
        string userId = TestAuthenticationHandler.TestUserId,
        int prepTimeMinutes = 10,
        int cookTimeMinutes = 20,
        string? yield = null,
        List<StepDto>? steps = null,
        List<(Ingredient Ingredient, decimal? Quantity, Unit? Unit)>? ingredients = null)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            Description = description ?? $"A test recipe for {name}.",
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            Yield = yield ?? "4 servings",
            StepsJson = JsonSerializer.Serialize(steps ?? new List<StepDto>
            {
                new() { Order = 1, Description = $"Prepare {name} using provided ingredients." },
                new() { Order = 2, Description = "Cook/assemble as needed." }
            }),
            CreatedBy = userId,
            RecipeIngredients = new List<RecipeIngredient>()
        };

        if (ingredients != null && ingredients.Count != 0)
        {
            foreach (var (ingredient, quantity, unit) in ingredients)
            {
                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    IngredientId = ingredient.Id,
                    Quantity = quantity ?? 1,
                    Unit = unit ?? Unit.Gram
                });
            }
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        await dbContext.Recipes.AddAsync(recipe);
        await dbContext.SaveChangesAsync();

        return recipe;
    }

    public async Task<Prep> SeedPrepAsync(
        Recipe recipe,
        string summaryNotes = "Test prep",
        int? prepTimeMinutes = 5,
        int? cookTimeMinutes = 10,
        List<StepDto>? steps = null,
        List<(Ingredient Ingredient, decimal? Quantity, Unit? Unit, string? Notes, PrepIngredientStatus Status)>?
            ingredients = null)
    {
        var prepSteps = steps ?? [new() { Order = 1, Description = "Default prep step." }];

        var prep = new Prep
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            UserId = recipe.UserId,
            SummaryNotes = summaryNotes,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            StepsJson = JsonSerializer.Serialize(prepSteps),
            CreatedAt = DateTimeOffset.UtcNow,
            PrepIngredients = []
        };

        if (ingredients != null && ingredients.Count != 0)
        {
            foreach (var (ingredient, quantity, unit, notes, status) in ingredients)
            {
                prep.PrepIngredients.Add(new PrepIngredient
                {
                    Id = Guid.NewGuid(),
                    IngredientId = ingredient.Id,
                    Quantity = quantity ?? 1,
                    Unit = unit ?? Unit.Gram,
                    Notes = notes,
                    Status = status,
                });
            }
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        dbContext.Attach(recipe);
        await dbContext.Preps.AddAsync(prep);
        await dbContext.SaveChangesAsync();

        return prep;
    }
}
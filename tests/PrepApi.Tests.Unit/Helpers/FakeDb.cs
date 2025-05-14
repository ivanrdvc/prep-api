using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Contracts;

namespace PrepApi.Tests.Unit.Helpers;

public class FakeDb(IUserContext userContext) : IDbContextFactory<PrepDb>
{
    public PrepDb CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PrepDb>()
            .UseInMemoryDatabase($"InMemoryTestDb-{DateTime.Now.ToFileTimeUtc()}")
            .Options;

        return new PrepDb(options, userContext);
    }

    public async Task<Recipe> SeedRecipeAsync(PrepDb context)
    {
        var ingredientId = Guid.NewGuid();
        context.Ingredients.Add(new Ingredient { Id = ingredientId, Name = "Test Ingredient" });
        await context.SaveChangesAsync();

        var recipe = new Recipe
        {
            Name = "Test Recipe",
            UserId = userContext.UserId ?? "test-user-id",
            Description = "Test Description",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            StepsJson = System.Text.Json.JsonSerializer.Serialize(new List<StepDto>
                { new() { Description = "Step 1", Order = 1 } }),
            RecipeIngredients = new List<RecipeIngredient>
            {
                new()
                {
                    RecipeId = Guid.NewGuid(),
                    IngredientId = ingredientId,
                    Quantity = 100,
                    Unit = PrepApi.Data.Unit.Gram
                }
            }
        };
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();
        return recipe;
    }

    public async Task<Prep> SeedPrepAsync(PrepDb context, Guid? recipeId = null)
    {
        var recipe = recipeId.HasValue
            ? await context.Recipes.FindAsync(recipeId.Value) ?? await SeedRecipeAsync(context)
            : await SeedRecipeAsync(context);

        var prep = new Prep
        {
            RecipeId = recipe.Id,
            Recipe = recipe,
            UserId = userContext.UserId ?? "test-user-id",
            SummaryNotes = "Test Prep",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 10,
            StepsJson = System.Text.Json.JsonSerializer.Serialize(new List<StepDto>
                { new() { Description = "Prep Step", Order = 1 } }),
            PrepIngredients = Prep.CreatePrepIngredients(
                new List<PrepIngredientInputDto>
                {
                    new()
                    {
                        IngredientId = recipe.RecipeIngredients.First().IngredientId, Quantity = 100,
                        Unit = PrepApi.Data.Unit.Gram
                    }
                },
                recipe)
        };

        context.Preps.Add(prep);
        await context.SaveChangesAsync();
        return prep;
    }

    public async Task<PrepRating> SeedPrepRatingAsync(PrepDb context, Guid prepId, string? userId = null, int overallRating = 5, bool liked = true)
    {
        var rating = new PrepRating
        {
            PrepId = prepId,
            UserId = userId ?? userContext.UserId ?? "test-user-id",
            Liked = liked,
            OverallRating = overallRating,
            TasteRating = 5,
            TextureRating = 5,
            AppearanceRating = 5,
            WhatWorkedWell = "Good taste",
            WhatToChange = "Nothing",
            AdditionalNotes = "Great!"
        };
        context.PrepRatings.Add(rating);
        await context.SaveChangesAsync();

        return rating;
    }

    private UpsertPrepRequest CreateUpsertPrepRequest(PrepDb context, Guid recipeId)
    {
        var recipe = context.Recipes.Include(r => r.RecipeIngredients).FirstOrDefault(r => r.Id == recipeId);
        var ingredientId = recipe?.RecipeIngredients.FirstOrDefault()?.IngredientId ?? Guid.NewGuid();

        return new UpsertPrepRequest
        {
            RecipeId = recipeId,
            SummaryNotes = "Prep notes",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            Steps = [new() { Description = "Step 1", Order = 1 }],
            PrepIngredients = [new() { IngredientId = ingredientId, Quantity = 100, Unit = PrepApi.Data.Unit.Gram }]
        };
    }

    public async Task<Recipe> SeedVariantRecipeAsync(PrepDb context, Guid originalRecipeId, string name, bool isFavoriteVariant)
    {
        var baseRecipe = await context.Recipes.FindAsync(originalRecipeId);

        var variant = new Recipe
        {
            Name = name,
            UserId = userContext.UserId ?? "test-user-id",
            Description = "desc",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            StepsJson = "[]",
            OriginalRecipeId = originalRecipeId,
            IsFavoriteVariant = isFavoriteVariant,
            RecipeIngredients = baseRecipe!.RecipeIngredients.Select(ri => new RecipeIngredient
            {
                IngredientId = ri.IngredientId,
                Quantity = ri.Quantity,
                Unit = ri.Unit
            }).ToList()
        };

        context.Recipes.Add(variant);
        await context.SaveChangesAsync();

        return variant;
    }
}
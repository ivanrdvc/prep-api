using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Preps;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Dtos;
using PrepApi.Shared.Entities;
using PrepApi.Shared.Services;

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
            UserId = userContext.ExternalId ?? "test-user-id",
            Description = "Test Description",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            StepsJson = JsonSerializer.Serialize(new List<StepDto> { new() { Description = "Step 1", Order = 1 } }),
            RecipeIngredients = new List<RecipeIngredient>
            {
                new()
                {
                    RecipeId = Guid.NewGuid(),
                    IngredientId = ingredientId,
                    Quantity = 100,
                    Unit = Shared.Entities.Unit.Gram
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
        var prepService = new PrepService();

        var prep = new Prep
        {
            RecipeId = recipe.Id,
            Recipe = recipe,
            UserId = userContext.ExternalId ?? "test-user-id",
            SummaryNotes = "Test Prep",
            PrepTimeMinutes = 5,
            CookTimeMinutes = 10,
            StepsJson = JsonSerializer.Serialize(new List<StepDto> { new() { Description = "Prep Step", Order = 1 } }),
            PrepIngredients = prepService.CreateIngredients(
                new List<PrepIngredientInputDto>
                {
                    new()
                    {
                        IngredientId = recipe.RecipeIngredients.First().IngredientId, Quantity = 100,
                        Unit = Shared.Entities.Unit.Gram
                    }
                },
                recipe)
        };

        context.Preps.Add(prep);
        await context.SaveChangesAsync();
        return prep;
    }

    public async Task<PrepRating> SeedPrepRatingAsync(PrepDb context, Guid prepId, string? userId = null, int overallRating = 5,
        bool liked = true)
    {
        var rating = new PrepRating
        {
            PrepId = prepId,
            UserId = userId ?? userContext.ExternalId ?? "test-user-id",
            Liked = liked,
            OverallRating = overallRating,
            DimensionsJson = JsonSerializer.Serialize(new Dictionary<string, int>
                { { "taste", 5 }, { "texture", 5 }, { "appearance", 5 } }),
            WhatWorkedWell = "Good taste",
            WhatToChange = "Nothing",
            AdditionalNotes = "Great!"
        };
        context.PrepRatings.Add(rating);
        await context.SaveChangesAsync();

        return rating;
    }

    public async Task<Recipe> SeedVariantRecipeAsync(PrepDb context, Guid originalRecipeId, string name, bool isFavoriteVariant)
    {
        var baseRecipe = await context.Recipes.FindAsync(originalRecipeId);

        var variant = new Recipe
        {
            Name = name,
            UserId = userContext.ExternalId ?? "test-user-id",
            Description = "desc",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            StepsJson = "[]",
            OriginalRecipeId = originalRecipeId,
            IsFavoriteVariant = isFavoriteVariant,
            RecipeIngredients = baseRecipe!.RecipeIngredients!.Select(ri => new RecipeIngredient
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

    public async Task SeedDefaultRatingDimensionsAsync(PrepDb context)
    {
        var defaultDimensions = new[]
        {
            new RatingDimension { Key = "taste", DisplayName = "Taste", SortOrder = 1 },
            new RatingDimension { Key = "texture", DisplayName = "Texture", SortOrder = 2 },
            new RatingDimension { Key = "appearance", DisplayName = "Appearance", SortOrder = 3 }
        };

        await context.RatingDimensions.AddRangeAsync(defaultDimensions);
        await context.SaveChangesAsync();
    }
}
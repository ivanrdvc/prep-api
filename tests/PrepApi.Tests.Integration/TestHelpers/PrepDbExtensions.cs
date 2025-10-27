using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Ingredients;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Dtos;
using PrepApi.Users;

namespace PrepApi.Tests.Integration.TestHelpers;

/// <summary>
/// Extension methods for PrepDb to seed test data.
/// Can be used directly by unit or integration tests.
/// </summary>
public static class PrepDbTestExtensions
{
    public static async Task<User> SeedUserAsync(
        this PrepDb dbContext,
        Guid? userId = null,
        string? externalId = null,
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        PreferredUnits preferredUnits = PreferredUnits.Metric)
    {
        var user = new User
        {
            Id = userId ?? TestConstants.TestUserId,
            ExternalId = externalId ?? TestConstants.TestUserExternalId,
            Email = email ?? TestConstants.TestUserEmail,
            FirstName = firstName,
            LastName = lastName,
            PreferredUnits = preferredUnits,
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public static async Task<Dictionary<string, Ingredient>> SeedIngredientsAsync(
        this PrepDb dbContext,
        params string[] names)
    {
        var ingredients = names.Select(name => new Ingredient
        {
            Name = name,
            UserId = null, // null = shared ingredient
        }).ToList();

        await dbContext.Ingredients.AddRangeAsync(ingredients);
        await dbContext.SaveChangesAsync();

        return ingredients.ToDictionary(i => i.Name, i => i);
    }

    public static async Task<Recipe> SeedRecipeAsync(
        this PrepDb dbContext,
        Guid? userId = null,
        string name = "Test Recipe",
        string? description = null,
        int prepTimeMinutes = 10,
        int cookTimeMinutes = 20,
        string? yield = null,
        List<StepDto>? steps = null,
        List<(Ingredient Ingredient, decimal? Quantity, Unit? Unit)>? ingredients = null,
        List<Tag>? tags = null,
        Guid? originalRecipeId = null,
        bool isFavoriteVariant = false)
    {
        var actualUserId = userId ?? TestConstants.TestUserId;

        var recipe = new Recipe
        {
            Name = name,
            UserId = actualUserId,
            Description = description ?? $"A test recipe for {name}.",
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            Yield = yield ?? "4 servings",
            StepsJson = JsonSerializer.Serialize(steps ??
            [
                new() { Order = 1, Description = $"Prepare {name} using provided ingredients." },
                new() { Order = 2, Description = "Cook/assemble as needed." }
            ]),
            CreatedBy = actualUserId,
            OriginalRecipeId = originalRecipeId,
            IsFavoriteVariant = isFavoriteVariant,
            RecipeIngredients = ingredients?.Select(x => new RecipeIngredient
            {
                IngredientId = x.Ingredient.Id,
                Quantity = x.Quantity ?? 1,
                Unit = x.Unit ?? Unit.Gram
            }).ToList() ?? []
        };

        if (tags?.Any() == true)
        {
            foreach (var tag in tags)
            {
                recipe.RecipeTags.Add(new RecipeTag
                {
                    RecipeId = recipe.Id,
                    TagId = tag.Id
                });
            }
        }

        await dbContext.Recipes.AddAsync(recipe);
        await dbContext.SaveChangesAsync();
        return recipe;
    }

    public static async Task<Prep> SeedPrepAsync(
        this PrepDb dbContext,
        Recipe recipe,
        string summaryNotes = "Test prep",
        int? prepTimeMinutes = 5,
        int? cookTimeMinutes = 10,
        List<StepDto>? steps = null,
        List<(Ingredient Ingredient, decimal? Quantity, Unit? Unit, string? Notes, PrepIngredientStatus Status)>? ingredients =
            null)
    {
        var prepSteps = steps ?? [new() { Order = 1, Description = "Default prep step." }];

        var prep = new Prep
        {
            RecipeId = recipe.Id,
            UserId = recipe.UserId,
            SummaryNotes = summaryNotes,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            StepsJson = JsonSerializer.Serialize(prepSteps),
            CreatedAt = DateTimeOffset.UtcNow,
            PrepIngredients = ingredients?.Select(x => new PrepIngredient
            {
                IngredientId = x.Ingredient.Id,
                Quantity = x.Quantity ?? 1,
                Unit = x.Unit ?? Unit.Gram,
                Notes = x.Notes,
                Status = x.Status
            }).ToList() ?? []
        };

        dbContext.Attach(recipe);
        await dbContext.Preps.AddAsync(prep);
        await dbContext.SaveChangesAsync();
        return prep;
    }

    public static async Task<Dictionary<string, Tag>> SeedTagsAsync(
        this PrepDb dbContext,
        Guid userId,
        params string[] names)
    {
        var result = new Dictionary<string, Tag>();
        var tagsToAdd = new List<Tag>();

        foreach (var name in names)
        {
            var existingTag = await dbContext.Tags
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == name);

            if (existingTag != null)
            {
                result[name] = existingTag;
            }
            else
            {
                var newTag = new Tag
                {
                    Name = name,
                    UserId = userId
                };
                tagsToAdd.Add(newTag);
                result[name] = newTag;
            }
        }

        if (tagsToAdd.Count > 0)
        {
            await dbContext.Tags.AddRangeAsync(tagsToAdd);
            await dbContext.SaveChangesAsync();
        }

        return result;
    }

    public static async Task<PrepRating> SeedPrepRatingAsync(
        this PrepDb dbContext,
        Guid prepId,
        Guid userId,
        int overallRating = 5,
        bool liked = true,
        string? whatWorkedWell = null,
        string? whatToChange = null,
        string? additionalNotes = null)
    {
        var rating = new PrepRating
        {
            PrepId = prepId,
            UserId = userId,
            Liked = liked,
            OverallRating = overallRating,
            DimensionsJson = JsonSerializer.Serialize(new Dictionary<string, int>
                { { "taste", 5 }, { "texture", 5 }, { "appearance", 5 } }),
            WhatWorkedWell = whatWorkedWell ?? "Good taste",
            WhatToChange = whatToChange ?? "Nothing",
            AdditionalNotes = additionalNotes ?? "Great!"
        };

        await dbContext.PrepRatings.AddAsync(rating);
        await dbContext.SaveChangesAsync();
        return rating;
    }

    public static async Task SeedDefaultRatingDimensionsAsync(this PrepDb dbContext)
    {
        var defaultDimensions = new[]
        {
            new RatingDimension { Key = "taste", DisplayName = "Taste", SortOrder = 1 },
            new RatingDimension { Key = "texture", DisplayName = "Texture", SortOrder = 2 },
            new RatingDimension { Key = "appearance", DisplayName = "Appearance", SortOrder = 3 }
        };

        await dbContext.RatingDimensions.AddRangeAsync(defaultDimensions);
        await dbContext.SaveChangesAsync();
    }

    public static async Task<Recipe> SeedVariantRecipeAsync(
        this PrepDb dbContext,
        Guid originalRecipeId,
        string name,
        bool isFavoriteVariant = false)
    {
        var baseRecipe = await dbContext.Recipes
            .IgnoreQueryFilters()
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == originalRecipeId);

        if (baseRecipe == null)
            throw new InvalidOperationException($"Base recipe with ID {originalRecipeId} not found");

        var variant = new Recipe
        {
            Name = name,
            UserId = baseRecipe.UserId,
            Description = baseRecipe.Description,
            PrepTimeMinutes = baseRecipe.PrepTimeMinutes,
            CookTimeMinutes = baseRecipe.CookTimeMinutes,
            Yield = baseRecipe.Yield,
            StepsJson = baseRecipe.StepsJson,
            CreatedBy = baseRecipe.UserId,
            OriginalRecipeId = originalRecipeId,
            IsFavoriteVariant = isFavoriteVariant,
            RecipeIngredients = baseRecipe.RecipeIngredients.Select(ri => new RecipeIngredient
            {
                IngredientId = ri.IngredientId,
                Quantity = ri.Quantity,
                Unit = ri.Unit
            }).ToList()
        };

        await dbContext.Recipes.AddAsync(variant);
        await dbContext.SaveChangesAsync();

        return variant;
    }

    /// <summary>
    /// Seeds a complete test scenario with user, ingredients, and recipe
    /// </summary>
    public static async Task<(User User, Dictionary<string, Ingredient> Ingredients, Recipe Recipe)> SeedCompleteScenarioAsync(
        this PrepDb dbContext,
        string[]? ingredientNames = null,
        string recipeName = "Test Recipe")
    {
        var user = await dbContext.SeedUserAsync();
        var ingredients = await dbContext.SeedIngredientsAsync(ingredientNames ?? ["Flour", "Sugar", "Milk"]);
        var recipe = await dbContext.SeedRecipeAsync(
            userId: user.Id,
            name: recipeName,
            ingredients: ingredients.Values.Take(2).Select(i => (i, (decimal?)100, (Unit?)Unit.Gram)).ToList()
        );

        return (user, ingredients, recipe);
    }
}
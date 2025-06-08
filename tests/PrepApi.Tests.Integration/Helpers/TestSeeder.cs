using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PrepApi.Data;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Dtos;
using PrepApi.Shared.Entities;
using PrepApi.Users;

using Recipe = PrepApi.Recipes.Entities.Recipe;
using RecipeIngredient = PrepApi.Recipes.Entities.RecipeIngredient;

namespace PrepApi.Tests.Integration.Helpers;

public class TestSeeder(TestWebAppFactory factory)
{
    public async Task<User> SeedTestUserAsync(
        string userId,
        string email,
        string? firstName = null,
        string? lastName = null,
        PreferredUnits preferredUnits = PreferredUnits.Metric)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();

        var user = new User
        {
            ExternalId = userId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PreferredUnits = preferredUnits,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "TestSystem"
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

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
        string userId = TestConstants.TestUserId,
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
        var recipe = new Recipe
        {
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

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        dbContext.Attach(recipe);
        await dbContext.Preps.AddAsync(prep);
        await dbContext.SaveChangesAsync();

        return prep;
    }

    public async Task<Dictionary<string, Tag>> SeedTagsAsync(string userId, params string[] names)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();

        var result = new Dictionary<string, Tag>();
        var tagsToAdd = new List<Tag>();

        foreach (var name in names)
        {
            // Check if tag already exists to avoid duplicates
            var existingTag = await dbContext.Tags
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

        if (tagsToAdd.Count == 0)
        {
            return result;
        }

        await dbContext.Tags.AddRangeAsync(tagsToAdd);
        await dbContext.SaveChangesAsync();

        return result;
    }

    public async Task SeedDefaultRatingDimensionsAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
        var defaultDimensions = new[]
        {
            new RatingDimension { Key = "taste", DisplayName = "Taste", SortOrder = 1 },
            new RatingDimension { Key = "texture", DisplayName = "Texture", SortOrder = 2 },
            new RatingDimension { Key = "appearance", DisplayName = "Appearance", SortOrder = 3 }
        };
        await dbContext.RatingDimensions.AddRangeAsync(defaultDimensions);
        await dbContext.SaveChangesAsync();
    }
}
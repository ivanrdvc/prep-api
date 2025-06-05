using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Recipes.Entities;
using PrepApi.Recipes.Requests;

namespace PrepApi.Recipes;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/recipes").RequireAuthorization();

        api.MapPost("/", CreateRecipe);
        api.MapGet("/{id:guid}", GetRecipe);
        api.MapDelete("/{id:guid}", DeleteRecipe);
        api.MapPut("/{id:guid}", UpdateRecipe);

        api.MapPost("{prepId:guid}/variants", CreateVariantFromPrep);
        api.MapPut("{id:guid}/favorite", SetFavoriteVariant);

        return api;
    }

    public static async Task<Results<Ok<RecipeDto>, NotFound>> GetRecipe(
        [FromRoute] Guid id,
        PrepDb db,
        IUserContext userContext)
    {
        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
            .Include(r => r.OriginalRecipe)
            .Include(r => r.Variants)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userContext.UserId);

        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(RecipeDto.FromRecipe(recipe));
    }

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateRecipe(
        [FromRoute] Guid id,
        [FromBody] UpsertRecipeRequest request,
        PrepDb db,
        IUserContext userContext,
        IValidator<UpsertRecipeRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var ingredientProblem = await ValidateRecipeIngredientsAsync(db, request.Ingredients);
        if (ingredientProblem != null)
        {
            return ingredientProblem!;
        }

        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeTags)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userContext.UserId);

        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        recipe.Name = request.Name;
        recipe.Description = request.Description;
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.Yield = request.Yield;
        recipe.StepsJson = JsonSerializer.Serialize(request.Steps);

        recipe.RecipeIngredients =
        [
            .. request.Ingredients.Select(i => new RecipeIngredient
            {
                IngredientId = i.IngredientId,
                Quantity = i.Quantity,
                Unit = i.Unit
            })
        ];

        recipe.RecipeTags = await CreateRecipeTagsFromIdsAsync(db, request.TagIds, userContext.UserId!);

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteRecipe(
        [FromRoute] Guid id,
        PrepDb db,
        IUserContext userContext)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userContext.UserId);

        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Created<Guid>, ValidationProblem, UnauthorizedHttpResult>> CreateRecipe(
        [FromBody] UpsertRecipeRequest request,
        PrepDb db,
        IUserContext userContext,
        IValidator<UpsertRecipeRequest> validator)
    {
        if (userContext.UserId is null)
        {
            return TypedResults.Unauthorized();
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var ingredientProblem = await ValidateRecipeIngredientsAsync(db, request.Ingredients);
        if (ingredientProblem != null)
        {
            return ingredientProblem!;
        }

        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            Yield = request.Yield,
            UserId = userContext.UserId,
            StepsJson = JsonSerializer.Serialize(request.Steps),
            RecipeIngredients = request.Ingredients.Select(ingredientDto => new RecipeIngredient
            {
                IngredientId = ingredientDto.IngredientId,
                Quantity = ingredientDto.Quantity,
                Unit = ingredientDto.Unit
            }).ToList(),
            RecipeTags = await CreateRecipeTagsFromIdsAsync(db, request.TagIds, userContext.UserId)
        };

        await db.Recipes.AddAsync(recipe);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/recipe/{recipe.Id}", recipe.Id);
    }

    public static async Task<Results<Created<Guid>, NotFound, ValidationProblem, UnauthorizedHttpResult>> CreateVariantFromPrep(
        [FromRoute] Guid prepId,
        [FromBody] CreateVariantFromPrepRequest request,
        PrepDb db,
        IUserContext userContext)
    {
        if (userContext.UserId is null)
        {
            return TypedResults.Unauthorized();
        }

        var prep = await db.Preps
            .Include(p => p.Recipe)
            .ThenInclude(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
            .Include(p => p.PrepIngredients)
            .ThenInclude(pi => pi.Ingredient)
            .FirstOrDefaultAsync(p => p.Id == prepId && p.UserId == userContext.UserId);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        var originalRecipe = prep.Recipe;
        if (request.SetAsFavorite)
        {
            var existingFavorite = await db.Recipes
                .Where(r => r.OriginalRecipeId == originalRecipe.Id &&
                            r.IsFavoriteVariant &&
                            r.UserId == userContext.UserId)
                .FirstOrDefaultAsync();
            if (existingFavorite != null)
            {
                existingFavorite.IsFavoriteVariant = false;
            }
        }

        var variant = new Recipe
        {
            Name = request.Name,
            Description = originalRecipe.Description,
            UserId = userContext.UserId,
            PrepTimeMinutes = prep.PrepTimeMinutes ?? originalRecipe.PrepTimeMinutes,
            CookTimeMinutes = prep.CookTimeMinutes ?? originalRecipe.CookTimeMinutes,
            Yield = originalRecipe.Yield, // should start with original or new yield?
            StepsJson = prep.StepsJson,
            OriginalRecipeId = originalRecipe.Id,
            IsFavoriteVariant = request.SetAsFavorite,
            RecipeIngredients = prep.PrepIngredients.Select(pi => new RecipeIngredient
            {
                IngredientId = pi.IngredientId,
                Quantity = pi.Quantity,
                Unit = pi.Unit
            }).ToList(),
            RecipeTags = originalRecipe.RecipeTags.Select(rt => new RecipeTag
            {
                TagId = rt.TagId
            }).ToList()
        };

        await db.Recipes.AddAsync(variant);

        prep.CreatedNewRecipeId = variant.Id;

        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/recipes/{prepId}/variants/{variant.Id}", variant.Id);
    }

    public static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> SetFavoriteVariant(
        [FromRoute] Guid id,
        PrepDb db,
        IUserContext userContext)
    {
        var variant = await db.Recipes
            .Include(r => r.OriginalRecipe)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userContext.UserId);

        if (variant?.OriginalRecipeId is null)
        {
            return TypedResults.NotFound();
        }

        var existingFavorite = await db.Recipes
            .Where(r => r.OriginalRecipeId == variant.OriginalRecipeId && r.IsFavoriteVariant)
            .FirstOrDefaultAsync();
        if (existingFavorite != null)
        {
            existingFavorite.IsFavoriteVariant = false;
        }

        variant.IsFavoriteVariant = true;

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<List<RecipeTag>> CreateRecipeTagsFromIdsAsync(
        PrepDb db,
        List<Guid>? tagIds,
        string userId)
    {
        if (tagIds == null || tagIds.Count == 0)
        {
            return [];
        }

        var distinctTagIds = tagIds.Distinct().ToList();
        var validTagIds = await db.Tags
            .Where(t => t.UserId == userId && distinctTagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        return validTagIds.Select(tagId => new RecipeTag
        {
            TagId = tagId
        }).ToList();
    }

    private static async Task<ValidationProblem?> ValidateRecipeIngredientsAsync(
        PrepDb db,
        IEnumerable<RecipeIngredientInputDto> requestedIngredients)
    {
        var requestedIngredientIds = requestedIngredients.Select(i => i.IngredientId).Distinct().ToList();
        if (requestedIngredientIds.Count == 0)
        {
            return null;
        }

        var existingIngredientCount = await db.Ingredients
            .AsNoTracking()
            .CountAsync(ing => requestedIngredientIds.Contains(ing.Id));

        if (existingIngredientCount == requestedIngredientIds.Count)
        {
            return null;
        }

        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            { "Ingredients", ["One or more specified ingredients do not exist."] }
        });
    }
}
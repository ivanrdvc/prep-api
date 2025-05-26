using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Endpoints;

public static class RecipeVariantEndpoints
{
    public static IEndpointRouteBuilder MapRecipeVariantEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapPost("{prepId:guid}/variants", CreateVariantFromPrep);
        group.MapPut("{id:guid}/favorite", SetFavoriteVariant);

        return group;
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
}
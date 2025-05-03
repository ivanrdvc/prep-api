using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/recipes").RequireAuthorization();

        api.MapPost("/", CreateRecipe);
        api.MapGet("/{id:guid}", GetRecipe);
        api.MapDelete("/{id:guid}", DeleteRecipe);
        api.MapPut("/{id:guid}", UpdateRecipe);

        return api;
    }

    public static async Task<Results<Ok<RecipeDto>, NotFound>> GetRecipe(
        [FromRoute] Guid id,
        PrepDb db,
        UserContext userContext)
    {
        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
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
        [FromBody] UpdateRecipeRequest request,
        PrepDb db,
        UserContext userContext,
        IValidator<UpdateRecipeRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var (ingredientsValid, ingredientProblem) = await ValidateIngredientsExistAsync(db, request.Ingredients);
        if (!ingredientsValid)
        {
            return ingredientProblem!;
        }

        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userContext.UserId);

        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        recipe.Name = request.Name;
        recipe.Description = request.Description;
        recipe.PrepTime = request.PrepTime;
        recipe.CookTime = request.CookTime;
        recipe.Yield = request.Yield;
        recipe.StepsJson = JsonSerializer.Serialize(request.Steps);
        
        recipe.RecipeIngredients.Clear();
        foreach (var ingredientDto in request.Ingredients)
        {
            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Recipe = recipe,
                IngredientId = ingredientDto.IngredientId,
                Quantity = ingredientDto.Quantity,
                Unit = ingredientDto.Unit
            });
        }

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteRecipe(
        [FromRoute] Guid id,
        PrepDb db,
        UserContext userContext)
    {
        var recipe = await db.Recipes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userContext.UserId);

        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Created<Guid>, ValidationProblem, UnauthorizedHttpResult>> CreateRecipe(
        [FromBody] CreateRecipeRequest request,
        PrepDb db,
        UserContext userContext,
        IValidator<CreateRecipeRequest> validator)
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

        var (ingredientsValid, ingredientProblem) = await ValidateIngredientsExistAsync(db, request.Ingredients);
        if (!ingredientsValid)
        {
            return ingredientProblem!;
        }

        var recipe = new Recipe
        {
            Name = request.Name,
            Description = request.Description,
            PrepTime = request.PrepTime,
            CookTime = request.CookTime,
            Yield = request.Yield,
            UserId = userContext.UserId,
            StepsJson = JsonSerializer.Serialize(request.Steps)
        };

        recipe.RecipeIngredients = request.Ingredients.Select(ingredientDto => new RecipeIngredient
        {
            Recipe = recipe,
            IngredientId = ingredientDto.IngredientId,
            Quantity = ingredientDto.Quantity,
            Unit = ingredientDto.Unit
        }).ToList();

        await db.Recipes.AddAsync(recipe);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/recipe/{recipe.Id}", recipe.Id);
    }

    private static async Task<(bool IsValid, ValidationProblem? Problem)> ValidateIngredientsExistAsync(
        PrepDb db,
        IEnumerable<RecipeIngredientInputDto> requestedIngredients)
    {
        var requestedIngredientIds = requestedIngredients.Select(i => i.IngredientId).Distinct().ToList();
        var existingIngredientCount = await db.Ingredients
            .AsNoTracking()
            .CountAsync(ing => requestedIngredientIds.Contains(ing.Id));

        if (existingIngredientCount == requestedIngredientIds.Count)
        {
            return (true, null);
        }

        var problem = TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            { "Ingredients", ["One or more specified ingredients do not exist."] }
        });

        return (false, problem);
    }
}
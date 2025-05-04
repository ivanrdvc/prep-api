using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi;

public static class PrepEndpoints
{
    public static IEndpointRouteBuilder MapPrepEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/preps", CreatePrep);

        return app;
    }

    public static async Task<Results<Created<Guid>, NotFound<string>, ValidationProblem, UnauthorizedHttpResult>>
        CreatePrep(
            [FromBody] CreatePrepRequest request,
            PrepDb db,
            UserContext userContext,
            IValidator<CreatePrepRequest> validator)
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

        var recipe = await db.Recipes
            .Include(r => r.RecipeIngredients)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId);
        
        if (recipe == null)
        {
            return TypedResults.NotFound($"Base Recipe with ID {request.RecipeId} not found.");
        }

        var ingredientProblem = await ValidatePrepIngredientsAsync(db, request.PrepIngredients);
        if (ingredientProblem != null)
        {
            return ingredientProblem;
        }

        var prep = Prep.CreateWithVariations(request, recipe, userContext.UserId);

        await db.Preps.AddAsync(prep);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/preps/{prep.Id}", prep.Id);
    }
    
    private static async Task<ValidationProblem?> ValidatePrepIngredientsAsync(
        PrepDb db,
        IEnumerable<PrepIngredientInputDto> requestedIngredients)
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
            { "PrepIngredients", ["One or more specified ingredients do not exist."] }
        });
    }
}
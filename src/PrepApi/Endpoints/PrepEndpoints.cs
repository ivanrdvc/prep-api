using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Endpoints;

public static class PrepEndpoints
{
    public static IEndpointRouteBuilder MapPrepEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/preps", CreatePrep);
        app.MapGet("api/preps/recipe/{recipeId:guid}", GetPrepsByRecipe);
        app.MapGet("api/preps/{id:guid}", GetPrep);
        app.MapDelete("api/preps/{id:guid}", DeletePrep);
        app.MapPut("api/preps/{id:guid}", UpdatePrep);

        return app;
    }

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdatePrep(
        [FromRoute]
        Guid id,
        [FromBody]
        UpdatePrepRequest request,
        PrepDb db,
        UserContext userContext,
        IValidator<UpdatePrepRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var ingredientProblem = await ValidatePrepIngredientsAsync(db, request.PrepIngredients);
        if (ingredientProblem != null)
        {
            return ingredientProblem;
        }

        var prep = await db.Preps
            .Include(p => p.PrepIngredients)
            .Include(p => p.Recipe)
            .ThenInclude(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userContext.UserId);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        prep.SummaryNotes = request.SummaryNotes;
        prep.PrepTimeMinutes = request.PrepTimeMinutes;
        prep.CookTimeMinutes = request.CookTimeMinutes;
        prep.StepsJson = JsonSerializer.Serialize(request.Steps);
        prep.PrepIngredients = Prep.CreatePrepIngredients(request.PrepIngredients, prep.Recipe);

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<PaginatedItems<PrepSummaryDto>>, ValidationProblem>> GetPrepsByRecipe(
        [FromRoute]
        Guid recipeId,
        [AsParameters]
        PaginationRequest request,
        PrepDb db,
        UserContext userContext)
    {
        var query = db.Preps
            .AsNoTracking()
            .Where(p => p.UserId == userContext.UserId)
            .Where(p => p.RecipeId == recipeId);

        query = request.SortOrder == SortOrder.asc
            ? query.OrderBy(p => p.CreatedAt)
            : query.OrderByDescending(p => p.CreatedAt);

        var totalItems = await query.CountAsync();

        var itemsOnPage = await query.Select(p => new PrepSummaryDto
            {
                Id = p.Id,
                BaseRecipeName = p.Recipe.Name,
                SummaryNotes = p.SummaryNotes,
                PreparedAt = p.CreatedAt
            })
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var result = new PaginatedItems<PrepSummaryDto>(
            request.PageIndex,
            request.PageSize,
            totalItems,
            itemsOnPage);

        return TypedResults.Ok(result);
    }

    public static async Task<Results<Created<Guid>, NotFound<string>, ValidationProblem, UnauthorizedHttpResult>>
        CreatePrep(
            [FromBody]
            CreatePrepRequest request,
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

        var prep = new Prep
        {
            RecipeId = recipe.Id,
            UserId = userContext.UserId,
            SummaryNotes = request.SummaryNotes,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            StepsJson = JsonSerializer.Serialize(request.Steps),
            PrepIngredients = Prep.CreatePrepIngredients(request.PrepIngredients, recipe)
        };

        await db.Preps.AddAsync(prep);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/preps/{prep.Id}", prep.Id);
    }

    public static async Task<Results<Ok<PrepDto>, NotFound>> GetPrep(
        [FromRoute]
        Guid id,
        PrepDb db,
        UserContext userContext)
    {
        var prep = await db.Preps
            .Include(p => p.Recipe)
            .Include(p => p.PrepIngredients)
            .ThenInclude(pi => pi.Ingredient)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userContext.UserId);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(PrepDto.FromPrep(prep));
    }

    public static async Task<Results<NoContent, NotFound>> DeletePrep(
        [FromRoute]
        Guid id,
        PrepDb db,
        UserContext userContext)
    {
        var prep = await db.Preps
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userContext.UserId);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        db.Preps.Remove(prep);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
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
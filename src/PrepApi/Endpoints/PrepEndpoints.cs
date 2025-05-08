using System.ComponentModel;

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
        app.MapPost("/api/preps", CreatePrep);
        app.MapGet("api/preps/{id:guid}", GetPrep);
        app.MapGet("api/preps", GetPreps);
        app.MapDelete("api/preps/{id:guid}", DeletePrep);

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

    public static async Task<Results<Ok<PrepDto>, NotFound>> GetPrep(
        [FromRoute] Guid id,
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
        [FromRoute] Guid id,
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

    public static async Task<Results<Ok<PaginatedItems<PrepSummaryDto>>, ValidationProblem>> GetPreps(
        [AsParameters] PaginationRequest request,
        [FromQuery] [DefaultValue(SortOrder.Desc)] SortOrder sortOrder,
        PrepDb db,
        UserContext userContext)
    {
        var query = db.Preps
            .AsNoTracking()
            .Where(p => p.UserId == userContext.UserId);

        query = sortOrder == SortOrder.Asc
            ? query.OrderBy(p => p.CreatedAt)
            : query.OrderByDescending(p => p.CreatedAt);

        var totalItems = await query.CountAsync();

        var itemsOnPage = await query.Select(p => new PrepSummaryDto
            {
                Id = p.Id,
                BaseRecipeName = p.Recipe != null ? p.Recipe.Name : null,
                SummaryNotes = p.SummaryNotes,
                PreparedAt = p.CreatedAt
            })
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var result = new PaginatedItems<PrepSummaryDto>(request.PageIndex, request.PageSize, totalItems, itemsOnPage);

        return TypedResults.Ok(result);
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
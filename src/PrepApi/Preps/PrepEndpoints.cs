using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Authorization;
using PrepApi.Data;
using PrepApi.Ingredients;
using PrepApi.Preps.Entities;
using PrepApi.Preps.Requests;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Requests;

namespace PrepApi.Preps;

public static class PrepEndpoints
{
    public static IEndpointRouteBuilder MapPrepEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/preps");
        group.WithTags("Preps");
        group.RequireAuthorization(pb => pb.RequireCurrentUser());

        group.MapPost("/", CreatePrep);
        group.MapPut("{id:guid}", UpdatePrep);
        group.MapGet("{id:guid}", GetPrep);
        group.MapDelete("{id:guid}", DeletePrep);
        group.MapGet("recipe/{recipeId:guid}", GetPrepsByRecipe);

        group.MapPrepRatingEndpoints();

        return app;
    }

    public static async Task<Results<Created<Guid>, NotFound<string>, ValidationProblem>> CreatePrep(
        UpsertPrepRequest request,
        PrepDb db,
        IUserContext userContext,
        PrepService prepService,
        IValidator<UpsertPrepRequest> validator)
    {
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

        var (ingredients, ingredientValidationError) = await LoadAndValidateIngredientsAsync(
            db, request.PrepIngredients, recipe);

        if (ingredientValidationError != null)
        {
            return ingredientValidationError;
        }

        var prep = new Prep
        {
            RecipeId = recipe.Id,
            UserId = userContext.InternalId,
            SummaryNotes = request.SummaryNotes,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            StepsJson = JsonSerializer.Serialize(request.Steps),
            PrepIngredients = prepService.CreateIngredients(request.PrepIngredients, recipe)
        };

        prep.ChangeSummary = prepService.GetChangeSummary(prep, recipe, ingredients!);

        await db.Preps.AddAsync(prep);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/preps/{prep.Id}", prep.Id);
    }

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdatePrep(
        [FromRoute] Guid id,
        [FromBody] UpsertPrepRequest request,
        PrepDb db,
        PrepService prepService,
        IValidator<UpsertPrepRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var prep = await db.Preps
            .Include(p => p.PrepIngredients)
            .Include(p => p.Recipe)
            .ThenInclude(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        var (ingredients, ingredientValidationError) =
            await LoadAndValidateIngredientsAsync(db, request.PrepIngredients, prep.Recipe);
        if (ingredientValidationError != null)
        {
            return ingredientValidationError;
        }

        prep.SummaryNotes = request.SummaryNotes;
        prep.PrepTimeMinutes = request.PrepTimeMinutes;
        prep.CookTimeMinutes = request.CookTimeMinutes;
        prep.StepsJson = JsonSerializer.Serialize(request.Steps);
        prep.PrepIngredients = prepService.CreateIngredients(request.PrepIngredients, prep.Recipe);

        prep.ChangeSummary = prepService.GetChangeSummary(prep, prep.Recipe, ingredients!);

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<PaginatedItems<PrepSummaryDto>>, ValidationProblem>> GetPrepsByRecipe(
        [FromRoute] Guid recipeId,
        [AsParameters] PaginationRequest request,
        PrepDb db)
    {
        var query = db.Preps
            .AsNoTracking()
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

    public static async Task<Results<Ok<PrepDto>, NotFound>> GetPrep(
        [FromRoute] Guid id,
        PrepDb db)
    {
        var prep = await db.Preps
            .Include(p => p.Recipe)
            .Include(p => p.PrepIngredients)
            .ThenInclude(pi => pi.Ingredient)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(PrepDto.FromPrep(prep));
    }

    public static async Task<Results<NoContent, NotFound>> DeletePrep(
        [FromRoute] Guid id,
        PrepDb db)
    {
        var prep = await db.Preps
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prep is null)
        {
            return TypedResults.NotFound();
        }

        db.Preps.Remove(prep);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<(Dictionary<Guid, Ingredient>? ingredients, ValidationProblem? validationError)>
        LoadAndValidateIngredientsAsync(
            PrepDb db,
            IEnumerable<PrepIngredientInputDto> requestedIngredients,
            Recipe recipe)
    {
        var requestedIngredientIds = requestedIngredients.Select(i => i.IngredientId).Distinct().ToList();
        var recipeIngredientIds = recipe.RecipeIngredients.Select(ri => ri.IngredientId).ToList();
        var allIngredientIds = requestedIngredientIds.Union(recipeIngredientIds).ToList();

        if (allIngredientIds.Count == 0)
        {
            return (new Dictionary<Guid, Ingredient>(), null);
        }

        // Check that ingredients exist AND user has access to them (query filter handles access)
        var ingredients = await db.Ingredients
            .Where(i => allIngredientIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i);

        var missingIngredients = requestedIngredientIds.Except(ingredients.Keys).ToList();
        if (missingIngredients.Count == 0)
        {
            return (ingredients, null);
        }

        var validationError = TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            { "PrepIngredients", ["One or more specified ingredients do not exist or you don't have access to them."] }
        });
        return (null, validationError);
    }
}
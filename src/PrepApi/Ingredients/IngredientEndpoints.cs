using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Ingredients.Requests;
using PrepApi.Shared.Services;

namespace PrepApi.Ingredients;

public static class IngredientEndpoints
{
    public static IEndpointRouteBuilder MapIngredientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/ingredients")
            .WithTags("Ingredients")
            .RequireAuthorization();

        group.MapGet("search", SearchIngredients);
        group.MapPost("/", CreateIngredient);
        group.MapGet("/", GetUserIngredients);
        group.MapPut("/{id:guid}", UpdateIngredient);
        group.MapDelete("/{id:guid}", DeleteIngredient);
        return app;
    }

    public static async Task<Results<Ok<List<IngredientDto>>, BadRequest<string>>> SearchIngredients(
        [FromServices] IIngredientService ingredientService,
        [FromQuery] string query,
        IUserContext userContext)
    {
        if (string.IsNullOrWhiteSpace(query))
            return TypedResults.BadRequest("Query is required.");

        var userId = userContext.InternalId!.Value;
        var results = await ingredientService.SearchAsync(query, userId);

        return TypedResults.Ok(results);
    }

    public static async Task<Results<Created<IngredientDto>, BadRequest<string>, Conflict<string>>> CreateIngredient(
        [FromBody] UpsertIngredientRequest request,
        PrepDb db,
        IUserContext userContext)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest("Name is required.");

        var userId = userContext.InternalId!.Value;

        var exists = await db.Ingredients
            .AnyAsync(i => i.Name == request.Name && i.UserId == userId);

        if (exists)
            return TypedResults.Conflict($"You already have an ingredient named '{request.Name}'.");

        var ingredient = new Ingredient
        {
            Name = request.Name,
            Category = request.Category,
            UserId = userId
        };

        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();

        var dto = IngredientDto.FromIngredient(ingredient);
        return TypedResults.Created($"/api/ingredients/{ingredient.Id}", dto);
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>, Conflict<string>>> UpdateIngredient(
        [FromRoute] Guid id,
        [FromBody] UpsertIngredientRequest request,
        PrepDb db,
        IUserContext userContext)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest("Name is required.");

        var userId = userContext.InternalId!.Value;

        var ingredient = await db.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (ingredient is null)
            return TypedResults.NotFound();

        // Check if new name conflicts with another ingredient
        var nameConflict = await db.Ingredients
            .AnyAsync(i => i.Name == request.Name && i.UserId == userId && i.Id != id);

        if (nameConflict)
            return TypedResults.Conflict($"You already have an ingredient named '{request.Name}'.");

        // Update properties
        db.Entry(ingredient).CurrentValues.SetValues(new Ingredient
        {
            Id = ingredient.Id,
            Name = request.Name,
            Category = request.Category,
            UserId = ingredient.UserId
        });

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteIngredient(
        [FromRoute] Guid id,
        PrepDb db,
        IUserContext userContext)
    {
        var userId = userContext.InternalId!.Value;

        var ingredient = await db.Ingredients
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

        if (ingredient is null)
            return TypedResults.NotFound();

        db.Ingredients.Remove(ingredient);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    public static async Task<Ok<List<IngredientDto>>> GetUserIngredients(
        PrepDb db,
        IUserContext userContext,
        [AsParameters] GetIngredientsRequest request)
    {
        var userId = userContext.InternalId!.Value;

        var take = Math.Min(request.Take, 100);

        var query = db.Ingredients
            .Where(i => i.UserId == null || i.UserId == userId);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(i => i.Name.Contains(request.Name));
        }

        // Get both shared ingredients (UserId = null) and user's own ingredients
        var ingredients = await query
            .OrderBy(i => i.Name)
            .Skip(request.Skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        var dtos = ingredients.Select(IngredientDto.FromIngredient).ToList();
        return TypedResults.Ok(dtos);
    }
}
using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Recipes.Entities;
using PrepApi.Recipes.Requests;
using PrepApi.Shared;
using PrepApi.Shared.Entities;

namespace PrepApi.Recipes;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/tags").RequireAuthorization();

        api.MapPost("/", CreateTag);
        api.MapGet("/", GetTags);
        api.MapPut("/{id:guid}", UpdateTag);
        api.MapDelete("/{id:guid}", DeleteTag);

        return api;
    }

    private static async Task<Results<Created<TagDto>, ValidationProblem>> CreateTag(
        [FromBody]
        UpsertTagRequest request,
        PrepDb db,
        IUserContext userContext,
        IValidator<UpsertTagRequest> validator)
    {
        if (userContext.UserId is null)
        {
            TypedResults.Unauthorized();
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var validationProblem = await CheckForDuplicateTag(db, request.Name, userContext.UserId!);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var tag = new Tag
        {
            Name = request.Name,
            UserId = userContext.UserId!
        };

        await db.Tags.AddAsync(tag);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/tags/{tag.Id}", TagDto.FromTag(tag));
    }

    private static async Task<Ok<List<TagDto>>> GetTags(
        [FromQuery]
        string? term,
        PrepDb db,
        IUserContext userContext)
    {
        var query = db.Tags
            .AsNoTracking()
            .Where(t => t.UserId == userContext.UserId);

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(t => t.Name.ToLower().Contains(term.ToLower()));
        }

        var tags = await query
            .Select(t => TagDto.FromTag(t))
            .ToListAsync();

        return TypedResults.Ok(tags);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateTag(
        [FromRoute]
        Guid id,
        [FromBody]
        UpsertTagRequest request,
        PrepDb db,
        IUserContext userContext,
        IValidator<UpsertTagRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var tag = await db.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userContext.UserId);

        if (tag is null)
        {
            return TypedResults.NotFound();
        }

        var validationProblem = await CheckForDuplicateTag(db, request.Name, userContext.UserId!, id);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        tag.Name = request.Name;
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> DeleteTag(
        [FromRoute]
        Guid id,
        PrepDb db,
        IUserContext userContext)
    {
        var tag = await db.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userContext.UserId);

        if (tag is null)
        {
            return TypedResults.NotFound();
        }

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<ValidationProblem?> CheckForDuplicateTag(
        PrepDb db,
        string tagName,
        string userId,
        Guid? excludeId = null)
    {
        var query = db.Tags
            .AsNoTracking()
            .Where(t => t.Name == tagName && t.UserId == userId);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        var existingTag = await query.FirstOrDefaultAsync();

        if (existingTag is not null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Name", ["A tag with this name already exists."] }
            });
        }

        return null;
    }
}
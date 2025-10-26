using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Preps.Entities;
using PrepApi.Preps.Requests;
using PrepApi.Shared.Services;

namespace PrepApi.Preps;

public static class PrepRatingEndpoints
{
    public static IEndpointRouteBuilder MapPrepRatingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("{prepId:guid}/ratings")
            .RequireAuthorization()
            .WithTags("Prep Ratings");

        group.MapPost("/", CreatePrepRating);
        group.MapPut("/{id:guid}", UpdatePrepRating);
        group.MapGet("/", GetPrepRatings);

        return app;
    }

    public static async Task<Results<Created<Guid>, NotFound<string>, UnauthorizedHttpResult, ValidationProblem>>
        CreatePrepRating(
            Guid prepId,
            UpsertPrepRatingRequest request,
            PrepDb db,
            IUserContext userContext,
            RecipeInsightService recipeInsightService,
            IValidator<UpsertPrepRatingRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var prepInfo = await db.Preps
            .Where(p => p.Id == prepId)
            .Select(p => new
            {
                p.Id,
                p.RecipeId,
                ExistingRating = p.Ratings.FirstOrDefault(r => r.UserId == userContext.InternalId)
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (prepInfo is null)
        {
            return TypedResults.NotFound($"Prep with ID {prepId} not found.");
        }

        if (prepInfo.ExistingRating != null)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "PrepId", ["A rating for this prep by this user already exists."] }
            };
            return TypedResults.ValidationProblem(errors);
        }

        var dimensionsValidationResult = await ValidateDimensions(request.Dimensions, db);
        if (dimensionsValidationResult != null)
        {
            return dimensionsValidationResult;
        }

        var rating = new PrepRating
        {
            PrepId = prepId,
            UserId = userContext.InternalId!.Value,
            Liked = request.Liked,
            OverallRating = request.OverallRating,
            Dimensions = request.Dimensions,
            WhatWorkedWell = request.WhatWorkedWell,
            WhatToChange = request.WhatToChange,
            AdditionalNotes = request.AdditionalNotes
        };

        await db.PrepRatings.AddAsync(rating);
        await db.SaveChangesAsync();

        await recipeInsightService.CalculateAndUpsertInsights(prepInfo.RecipeId);

        return TypedResults.Created($"/api/preps/{prepId}/ratings", rating.Id);
    }

    public static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ValidationProblem>> UpdatePrepRating(
        Guid prepId,
        Guid id,
        UpsertPrepRatingRequest request,
        PrepDb db,
        IUserContext userContext,
        RecipeInsightService recipeInsightService,
        IValidator<UpsertPrepRatingRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var ratingInfo = await db.PrepRatings
            .Where(r => r.Id == id && r.PrepId == prepId && r.UserId == userContext.InternalId)
            .Select(r => new
            {
                Rating = r,
                RecipeId = r.Prep.RecipeId
            })
            .FirstOrDefaultAsync();

        if (ratingInfo is null)
        {
            return TypedResults.NotFound();
        }

        var dimensionsValidationResult = await ValidateDimensions(request.Dimensions, db);
        if (dimensionsValidationResult != null)
        {
            return dimensionsValidationResult;
        }

        var rating = ratingInfo.Rating;
        rating.Liked = request.Liked;
        rating.OverallRating = request.OverallRating;
        rating.Dimensions = request.Dimensions;
        rating.WhatWorkedWell = request.WhatWorkedWell;
        rating.WhatToChange = request.WhatToChange;
        rating.AdditionalNotes = request.AdditionalNotes;

        await db.SaveChangesAsync();

        await recipeInsightService.CalculateAndUpsertInsights(ratingInfo.RecipeId);

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<List<PrepRatingDto>>, NotFound>> GetPrepRatings(
        Guid prepId,
        PrepDb db)
    {
        var ratings = await db.PrepRatings
            .AsNoTracking()
            .Where(r => r.PrepId == prepId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var ratingDtos = ratings.Select(r => new PrepRatingDto
        {
            Id = r.Id,
            PrepId = r.PrepId,
            UserId = r.UserId,
            OverallRating = r.OverallRating,
            Liked = r.Liked,
            Dimensions = r.Dimensions,
            WhatWorkedWell = r.WhatWorkedWell,
            WhatToChange = r.WhatToChange,
            AdditionalNotes = r.AdditionalNotes,
            RatedAt = r.CreatedAt,
        }).ToList();

        return ratings.Count == 0 ? TypedResults.NotFound() : TypedResults.Ok(ratingDtos);
    }

    private static async Task<ValidationProblem?> ValidateDimensions(Dictionary<string, int> dimensions, PrepDb db)
    {
        if (dimensions.Count <= 0)
        {
            return null;
        }

        var knownDimensions = await db.RatingDimensions
            .Select(d => d.Key)
            .ToListAsync();

        var invalidDimensions = dimensions.Keys
            .Where(k => !knownDimensions.Contains(k))
            .ToList();

        if (invalidDimensions.Count > 0)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Dimensions", [string.Join(", ", invalidDimensions) + " are not valid rating dimensions."] }
            };
            return TypedResults.ValidationProblem(errors);
        }

        return null;
    }
}
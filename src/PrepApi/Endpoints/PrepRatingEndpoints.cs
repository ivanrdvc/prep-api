using System.Text.Json;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Endpoints;

public static class PrepRatingEndpoints
{
    public static IEndpointRouteBuilder MapPrepRatingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("{prepId}/ratings", CreatePrepRating);
        app.MapPut("{prepId}/ratings/{id:guid}", UpdatePrepRating);
        app.MapGet("{prepId}/ratings", GetPrepRatings);

        return app;
    }

    public static async Task<Results<Created<Guid>, NotFound<string>, UnauthorizedHttpResult, ValidationProblem>>
        CreatePrepRating(
            [FromRoute]
            Guid prepId,
            [FromBody]
            UpsertPrepRatingRequest request,
            PrepDb db,
            IUserContext userContext,
            IValidator<UpsertPrepRatingRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        if (userContext.UserId is null)
        {
            return TypedResults.Unauthorized();
        }

        var existingRating = await db.PrepRatings.FirstOrDefaultAsync(r => r.PrepId == prepId && r.UserId == userContext.UserId);
        if (existingRating != null)
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
            UserId = userContext.UserId!,
            Liked = request.Liked,
            OverallRating = request.OverallRating,
            DimensionsJson = request.Dimensions.Count > 0 ? JsonSerializer.Serialize(request.Dimensions) : null,
            WhatWorkedWell = request.WhatWorkedWell,
            WhatToChange = request.WhatToChange,
            AdditionalNotes = request.AdditionalNotes
        };

        await db.PrepRatings.AddAsync(rating);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/preps/{prepId}/ratings", rating.Id);
    }

    public static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ValidationProblem>> UpdatePrepRating(
        [FromRoute]
        Guid prepId,
        [FromRoute]
        Guid id,
        [FromBody]
        UpsertPrepRatingRequest request,
        PrepDb db,
        IUserContext userContext,
        IValidator<UpsertPrepRatingRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var rating = await db.PrepRatings.FirstOrDefaultAsync(r =>
            r.Id == id &&
            r.PrepId == prepId &&
            r.UserId == userContext.UserId);

        if (rating is null)
        {
            return TypedResults.NotFound();
        }

        var dimensionsValidationResult = await ValidateDimensions(request.Dimensions, db);
        if (dimensionsValidationResult != null)
        {
            return dimensionsValidationResult;
        }

        rating.Liked = request.Liked;
        rating.OverallRating = request.OverallRating;
        rating.DimensionsJson = request.Dimensions.Count > 0 ? JsonSerializer.Serialize(request.Dimensions) : null;
        rating.WhatWorkedWell = request.WhatWorkedWell;
        rating.WhatToChange = request.WhatToChange;
        rating.AdditionalNotes = request.AdditionalNotes;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<List<PrepRatingDto>>, NotFound>> GetPrepRatings(
        [FromRoute]
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
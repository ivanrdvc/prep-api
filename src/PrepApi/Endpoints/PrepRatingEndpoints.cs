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

    public static async Task<Results<Created<Guid>, NotFound<string>, UnauthorizedHttpResult, ValidationProblem>> CreatePrepRating(
        [FromRoute] Guid prepId,
        [FromBody] UpsertPrepRatingRequest request,
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

        var prep = await db.Preps.FirstOrDefaultAsync(x => x.Id == prepId && x.UserId == userContext.UserId);

        if (prep is null)
        {
            return TypedResults.NotFound($"Prep with ID {prepId} not found.");
        }

        var rating = new PrepRating
        {
            PrepId = prepId,
            UserId = userContext.UserId,
            Liked = request.Liked,
            OverallRating = request.OverallRating,
            TasteRating = request.TasteRating,
            TextureRating = request.TextureRating,
            AppearanceRating = request.AppearanceRating,
            AdditionalNotes = request.AdditionalNotes,
            WhatWorkedWell = request.WhatWorkedWell,
            WhatToChange = request.WhatToChange
        };

        await db.PrepRatings.AddAsync(rating);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/prep-ratings/{rating.Id}", rating.Id);
    }

    public static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult, ValidationProblem>> UpdatePrepRating(
        [FromRoute] Guid prepId,
        [FromRoute] Guid id,
        [FromBody] UpsertPrepRatingRequest request,
        PrepDb db,
        IUserContext userContext)
    {
        if (userContext.UserId is null)
        {
            return TypedResults.Unauthorized();
        }

        var rating = await db.PrepRatings.FirstOrDefaultAsync(r =>
            r.Id == id &&
            r.PrepId == prepId &&
            r.UserId == userContext.UserId);

        if (rating is null)
        {
            return TypedResults.NotFound();
        }

        rating.Liked = request.Liked;
        rating.OverallRating = request.OverallRating;
        rating.TasteRating = request.TasteRating;
        rating.TextureRating = request.TextureRating;
        rating.AppearanceRating = request.AppearanceRating;
        rating.WhatWorkedWell = request.WhatWorkedWell;
        rating.WhatToChange = request.WhatToChange;
        rating.AdditionalNotes = request.AdditionalNotes;

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<List<PrepRatingDto>>, NotFound>> GetPrepRatings(
        [FromRoute] Guid prepId,
        PrepDb db)
    {
        var ratings = await db.PrepRatings
            .AsNoTracking()
            .Where(r => r.PrepId == prepId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PrepRatingDto
            {
                Id = r.Id,
                PrepId = r.PrepId,
                UserId = r.UserId,
                OverallRating = r.OverallRating,
                Liked = r.Liked,
                TasteRating = r.TasteRating,
                TextureRating = r.TextureRating,
                AppearanceRating = r.AppearanceRating,
                WhatWorkedWell = r.WhatWorkedWell,
                WhatToChange = r.WhatToChange,
                AdditionalNotes = r.AdditionalNotes,
                RatedAt = r.CreatedAt,
            })
            .ToListAsync();

        return ratings.Count == 0 ? TypedResults.NotFound() : TypedResults.Ok(ratings);
    }
}

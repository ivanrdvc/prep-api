using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PrepApi.Shared.Services; // For UsdaFoodSearchResponse

namespace PrepApi.Preps;

public static class IngredientEndpoints
{
    public static IEndpointRouteBuilder MapIngredientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/ingredients")
            .WithTags("Ingredients")
            .RequireAuthorization();

        group.MapGet("search", SearchIngredients);
        return app;
    }

    public static async Task<Results<Ok<UsdaFoodSearchResponse>, BadRequest<string>>> SearchIngredients(
        [FromServices] UsdaApiService usdaApiService,
        [FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return TypedResults.BadRequest("Query is required.");
        var result = await usdaApiService.SearchFoundationFoodsAsync(query);
        if (result == null)
            return TypedResults.BadRequest("USDA API error.");
        return TypedResults.Ok(result);
    }
}

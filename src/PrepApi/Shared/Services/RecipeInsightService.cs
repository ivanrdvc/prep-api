using Microsoft.EntityFrameworkCore;

using PrepApi.Data;
using PrepApi.Recipes.Entities;

namespace PrepApi.Shared.Services;

public class RecipeInsightService(PrepDb db)
{
    public async Task CalculateAndUpsertInsights(Guid recipeId)
    {
        var recipe = await db.Recipes
            .Include(r => r.Preps)
            .ThenInclude(p => p.Ratings)
            .FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe == null)
        {
            return;
        }

        var ratings = recipe.Preps.SelectMany(p => p.Ratings).ToList();
        if (ratings.Count == 0)
        {
            return;
        }

        var averageOverallRating = ratings.Average(r => r.OverallRating);
        var totalRatings = ratings.Count;
        var totalPreparations = recipe.Preps.Count;

        var dimensionAverages = ratings
            .SelectMany(r => r.Dimensions)
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => g.Average(kvp => kvp.Value));

        var existingInsight = await db.RecipeInsights
            .FirstOrDefaultAsync(ri => ri.RecipeId == recipeId);

        if (existingInsight != null)
        {
            existingInsight.AverageOverallRating = averageOverallRating;
            existingInsight.TotalRatings = totalRatings;
            existingInsight.TotalPreparations = totalPreparations;
            existingInsight.DimensionAverages = dimensionAverages;
        }
        else
        {
            await db.RecipeInsights.AddAsync(new RecipeInsight
            {
                RecipeId = recipeId,
                AverageOverallRating = averageOverallRating,
                TotalRatings = totalRatings,
                TotalPreparations = totalPreparations,
                DimensionAverages = dimensionAverages
            });
        }

        await db.SaveChangesAsync();
    }
}
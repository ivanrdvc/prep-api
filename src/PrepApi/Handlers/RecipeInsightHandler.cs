using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Handlers;

public class RecipeInsightHandler(PrepDb db)
{
    public async Task<RecipeInsightResponse?> CalculateAndUpsertInsights(Guid recipeId)
    {
        var recipe = await db.Recipes
            .Include(r => r.Preps)
            .ThenInclude(p => p.Ratings)
            .FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe == null)
        {
            return null;
        }

        var ratings = recipe.Preps.SelectMany(p => p.Ratings).ToList();
        if (ratings.Count == 0)
        {
            return null;
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

        return new RecipeInsightResponse
        {
            RecipeId = recipeId,
            AverageOverallRating = averageOverallRating,
            TotalRatings = totalRatings,
            TotalPreparations = totalPreparations,
            DimensionAverages = dimensionAverages
        };
    }
}
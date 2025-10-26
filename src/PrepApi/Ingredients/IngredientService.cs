using Microsoft.EntityFrameworkCore;

using PrepApi.Data;

namespace PrepApi.Ingredients;

public interface IIngredientService
{
    Task<List<IngredientDto>> SearchAsync(string query, Guid userId);
}

public class IngredientService(PrepDb db) : IIngredientService
{
    public async Task<List<IngredientDto>> SearchAsync(string query, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var searchTerm = query.ToLower();

        var results = await db.Ingredients
            .Where(i => (i.UserId == null || i.UserId == userId) &&
                        EF.Functions.ILike(i.Name, $"%{searchTerm}%"))
            .OrderBy(i => i.Name)
            .Take(10)
            .Select(i => new IngredientDto
            {
                Id = i.Id,
                Name = i.Name,
                Category = i.Category,
                IsShared = i.UserId == null
            })
            .ToListAsync();

        return results;
    }
}
using PrepApi.Data;

using System.Text.Json;

namespace PrepApi.Contracts;

public record RecipeDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int PrepTime { get; init; }
    public required int CookTime { get; init; }
    public string? Yield { get; init; }
    public required List<IngredientDto> Ingredients { get; init; }
    public required List<StepDto> Steps { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static RecipeDto FromRecipe(Recipe recipe)
    {
        var ingredients = recipe.RecipeIngredients.Select(ri => new IngredientDto
        {
            IngredientId = ri.IngredientId,
            Name = ri.Ingredient?.Name ?? string.Empty,
            Quantity = ri.Quantity,
            Unit = ri.Unit
        }).ToList();
        
        var steps = JsonSerializer.Deserialize<List<StepDto>>(recipe.StepsJson) ?? [];

        return new RecipeDto
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Ingredients = ingredients,
            PrepTime = recipe.PrepTime,
            CookTime = recipe.CookTime,
            Steps = steps,
            Yield = recipe.Yield,
            CreatedAt = recipe.CreatedAt
        };
    }
}

public record IngredientDto
{
    public required Guid IngredientId { get; init; }
    public required string Name { get; init; }
    public required double Quantity { get; init; }
    public required Unit Unit { get; init; }
}

public record StepDto
{
    public required string Description { get; init; }

    public required short Order { get; init; }
}

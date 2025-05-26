using System.Text.Json;

using PrepApi.Data;

namespace PrepApi.Contracts;

public record RecipeDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int PrepTimeMinutes { get; init; }
    public required int CookTimeMinutes { get; init; }
    public string? Yield { get; init; }
    public required List<IngredientDto> Ingredients { get; init; }
    public required List<StepDto> Steps { get; init; }
    public List<TagDto> Tags { get; set; } = [];
    public required DateTimeOffset CreatedAt { get; init; }
    public Guid? OriginalRecipeId { get; init; }
    public string? OriginalRecipeName { get; init; }
    public bool IsFavoriteVariant { get; init; }
    public List<VariantSummaryDto> Variants { get; init; } = [];

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
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            Steps = steps,
            Tags = recipe.RecipeTags.Select(rt => TagDto.FromTag(rt.Tag)).ToList(),
            Yield = recipe.Yield,
            CreatedAt = recipe.CreatedAt,
            OriginalRecipeId = recipe.OriginalRecipeId,
            OriginalRecipeName = recipe.OriginalRecipe?.Name,
            IsFavoriteVariant = recipe.IsFavoriteVariant,
            Variants = recipe.Variants.Select(v => new VariantSummaryDto
            {
                Id = v.Id,
                Name = v.Name,
                IsFavorite = v.IsFavoriteVariant
            }).ToList()
        };
    }
}

public record IngredientDto
{
    public required Guid IngredientId { get; init; }
    public required string Name { get; init; }
    public required decimal Quantity { get; init; }
    public required Unit Unit { get; init; }
}

public record StepDto
{
    public required string Description { get; init; }
    public required short Order { get; init; }
}

public record VariantSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsFavorite { get; init; }
}
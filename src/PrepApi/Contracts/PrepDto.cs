using System.Text.Json;

using PrepApi.Data;

namespace PrepApi.Contracts;

public record PrepDto
{
    public Guid Id { get; init; }
    public Guid BaseRecipeId { get; init; }
    public string? BaseRecipeName { get; init; }
    public string? SummaryNotes { get; init; }
    public int? PrepTimeMinutes { get; init; }
    public int? CookTimeMinutes { get; init; }
    public required List<PrepIngredientDto> PrepIngredients { get; init; }
    public required List<StepDto> Steps { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedNewRecipeId { get; init; }

    public static PrepDto FromPrep(Prep prep)
    {
        var prepIngredients = prep.PrepIngredients.Select(pi => new PrepIngredientDto
        {
            IngredientId = pi.IngredientId,
            Name = pi.Ingredient?.Name ?? string.Empty,
            Quantity = pi.Quantity,
            Unit = pi.Unit,
            Notes = pi.Notes,
            Status = pi.Status
        }).ToList();

        var steps = JsonSerializer.Deserialize<List<StepDto>>(prep.StepsJson) ?? [];

        return new PrepDto
        {
            Id = prep.Id,
            BaseRecipeId = prep.RecipeId,
            BaseRecipeName = prep.Recipe?.Name,
            SummaryNotes = prep.SummaryNotes,
            PrepTimeMinutes = prep.PrepTimeMinutes,
            CookTimeMinutes = prep.CookTimeMinutes,
            PrepIngredients = prepIngredients,
            Steps = steps,
            CreatedAt = prep.CreatedAt,
            CreatedNewRecipeId = prep.CreatedNewRecipeId
        };
    }
}

public record PrepIngredientDto
{
    public required Guid IngredientId { get; init; }
    public required string Name { get; init; }
    public required decimal Quantity { get; init; }
    public required Unit Unit { get; init; }
    public string? Notes { get; init; }
    public required PrepIngredientStatus Status { get; init; }
}

public record PrepSummaryDto
{
    public Guid Id { get; init; }
    public string? BaseRecipeName { get; init; }
    public string? SummaryNotes { get; init; }
    public DateTimeOffset PreparedAt { get; init; }
}
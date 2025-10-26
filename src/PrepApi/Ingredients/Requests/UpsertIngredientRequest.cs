namespace PrepApi.Ingredients.Requests;

public record UpsertIngredientRequest
{
    public required string Name { get; init; }
    public string? Category { get; init; }
}
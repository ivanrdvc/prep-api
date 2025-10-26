namespace PrepApi.Ingredients;

public record IngredientDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
    public bool IsShared { get; init; }

    public static IngredientDto FromIngredient(Ingredient ingredient)
    {
        return new IngredientDto
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            Category = ingredient.Category,
            IsShared = ingredient.UserId == null
        };
    }
}
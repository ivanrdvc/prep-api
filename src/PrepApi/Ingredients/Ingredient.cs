using PrepApi.Data;

namespace PrepApi.Ingredients;

public class Ingredient : Entity
{
    public required string Name { get; init; }
    public Guid? UserId { get; init; } // Null = shared ingredient, value = user-specific
    public string? Category { get; init; }
}
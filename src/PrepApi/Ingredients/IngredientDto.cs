namespace PrepApi.Ingredients;

public class IngredientDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
}

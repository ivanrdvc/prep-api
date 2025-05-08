namespace PrepApi.Data;

public class Recipe : Entity
{
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public string? Yield { get; set; }
    public List<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public required string StepsJson { get; set; }
}

public class RecipeIngredient
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public Guid IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public decimal Quantity { get; set; }
    public Unit Unit { get; set; }
}

public enum Unit
{
    Whole = 1,
    Gram = 2,
    Kilogram = 3,
    Milliliter = 4
}
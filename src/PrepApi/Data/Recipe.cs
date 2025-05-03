namespace PrepApi.Data;

public class Recipe : Entity
{
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public required string Description { get; set; }
    public int PrepTime { get; set; }
    public int CookTime { get; set; }
    public string? Yield { get; set; }
    public List<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public required string StepsJson { get; set; }
}


public class Ingredient : Entity
{
    public required string Name { get;  set; }
    public string? UserId { get; set; }
}

public class RecipeIngredient
{
    public Guid RecipeId { get; set; }
    public required Recipe Recipe { get;  set; }
    public Guid IngredientId { get;  set; }
    public Ingredient? Ingredient { get; set; } 
    public double Quantity { get;  set; }
    public Unit Unit { get;  set; }
}

public enum Unit
{
    Whole = 1,
    Gram = 2,
    Kilogram = 3,
    Milliliter = 4
}
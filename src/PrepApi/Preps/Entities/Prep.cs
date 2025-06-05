using PrepApi.Data;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Entities;

namespace PrepApi.Preps.Entities;

public class Prep : Entity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public required string UserId { get; set; }
    public User User { get; set; } = null!;
    public string? SummaryNotes { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public ICollection<PrepIngredient> PrepIngredients { get; set; } = [];
    public required string StepsJson { get; set; }
    public Guid? CreatedNewRecipeId { get; set; }
    public Recipe? CreatedNewRecipe { get; set; }
    public ICollection<PrepRating> Ratings { get; set; } = [];
    public string? ChangeSummary { get; set; }
}

public class PrepIngredient
{
    public Guid Id { get; set; }
    public Guid PrepId { get; set; }
    public Prep? Prep { get; set; }
    public Guid IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public decimal Quantity { get; set; }
    public Unit Unit { get; set; }
    public string? Notes { get; set; }
    public PrepIngredientStatus Status { get; set; }
}

public enum PrepIngredientStatus
{
    Added = 1,
    Kept = 2,
    Modified = 3
}
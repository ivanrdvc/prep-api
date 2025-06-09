using PrepApi.Data;
using PrepApi.Recipes.Entities;
using PrepApi.Users;

namespace PrepApi.Preps.Entities;

public class Prep : Entity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public Guid UserId { get; set; }
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
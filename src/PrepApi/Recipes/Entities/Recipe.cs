using PrepApi.Data;
using PrepApi.Preps.Entities;
using PrepApi.Shared.Entities;

namespace PrepApi.Recipes.Entities;

public class Recipe : Entity
{
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public User User { get; set; } = null!;
    public required string Description { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public string? Yield { get; set; }
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public required string StepsJson { get; set; }
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
    public Guid? OriginalRecipeId { get; set; }
    public Recipe? OriginalRecipe { get; set; }
    public ICollection<Recipe> Variants { get; set; } = [];
    public bool IsFavoriteVariant { get; set; }
    public ICollection<Prep> Preps { get; private set; } = [];
}
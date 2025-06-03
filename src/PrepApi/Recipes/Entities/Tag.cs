using PrepApi.Data;

namespace PrepApi.Recipes.Entities;

public class Tag : Entity
{
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
}
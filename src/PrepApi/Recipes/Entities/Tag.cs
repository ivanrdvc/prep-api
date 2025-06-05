using PrepApi.Data;
using PrepApi.Shared.Entities;

namespace PrepApi.Recipes.Entities;

public class Tag : Entity
{
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
}
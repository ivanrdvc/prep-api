using PrepApi.Data;
using PrepApi.Users;

namespace PrepApi.Recipes.Entities;

public class Tag : Entity
{
    public required string Name { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
}
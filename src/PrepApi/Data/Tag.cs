namespace PrepApi.Data;

public class Tag : Entity
{
    public required string Name { get; set; }
    public required string UserId { get; set; }
    public ICollection<RecipeTag> RecipeTags { get; set; } = [];
}

public class RecipeTag
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
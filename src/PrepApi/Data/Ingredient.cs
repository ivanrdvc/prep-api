namespace PrepApi.Data;

public class Ingredient : Entity
{
    public required string Name { get; set; }
    public Guid UserId { get; set; }
}
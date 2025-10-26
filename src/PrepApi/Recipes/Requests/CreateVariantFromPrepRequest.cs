namespace PrepApi.Recipes.Requests;

public class CreateVariantFromPrepRequest
{
    public required string Name { get; init; }
    public bool SetAsFavorite { get; init; }
}
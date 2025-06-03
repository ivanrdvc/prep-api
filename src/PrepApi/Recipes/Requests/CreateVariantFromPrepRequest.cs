namespace PrepApi.Recipes.Requests;

public class CreateVariantFromPrepRequest
{
    public required string Name { get; set; }
    public bool SetAsFavorite { get; set; }
}
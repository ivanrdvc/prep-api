namespace PrepApi.Contracts;

public class CreateVariantFromPrepRequest
{
    public required string Name { get; set; }
    public bool SetAsFavorite { get; set; }
}
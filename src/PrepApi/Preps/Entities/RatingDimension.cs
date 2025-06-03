using PrepApi.Data;

namespace PrepApi.Preps.Entities;

public class RatingDimension : Entity
{
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
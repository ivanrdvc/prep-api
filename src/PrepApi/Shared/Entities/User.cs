using PrepApi.Data;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;

namespace PrepApi.Shared.Entities;

public class User : Entity
{
    public new required string Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public PreferredUnits PreferredUnits { get; set; } = PreferredUnits.Metric;
}

public enum PreferredUnits
{
    Metric = 0,
    Imperial = 1
}
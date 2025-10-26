using PrepApi.Data;

namespace PrepApi.Users;

public class User : Entity
{
    public required string ExternalId { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public PreferredUnits PreferredUnits { get; set; } = PreferredUnits.Metric;
}

public enum PreferredUnits
{
    Metric = 0,
    Imperial = 1
}
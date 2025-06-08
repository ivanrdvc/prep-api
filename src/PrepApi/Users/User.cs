using PrepApi.Data;

namespace PrepApi.Users;

public class User : Entity
{
    public new required string ExternalId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public PreferredUnits PreferredUnits { get; set; } = PreferredUnits.Metric;
}

public enum PreferredUnits
{
    Metric = 0,
    Imperial = 1
}
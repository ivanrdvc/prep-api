namespace PrepApi.Users;

public record UserDto
{
    public required string Id { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public PreferredUnits PreferredUnits { get; init; }

    public static UserDto FromUser(User user) => new()
    {
        Id = user.ExternalId,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PreferredUnits = user.PreferredUnits
    };
}
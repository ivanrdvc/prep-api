namespace PrepApi.Users;

public record UserDto
{
    public required Guid Id { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public PreferredUnits PreferredUnits { get; init; }

    public static UserDto FromUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PreferredUnits = user.PreferredUnits
    };
}
namespace PrepApi.Users;

public class UserDto
{
    public required string Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public PreferredUnits PreferredUnits { get; set; }

    public static UserDto FromUser(User user) => new()
    {
        Id = user.ExternalId,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PreferredUnits = user.PreferredUnits
    };
}
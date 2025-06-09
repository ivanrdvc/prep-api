namespace PrepApi.Users;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public PreferredUnits PreferredUnits { get; set; }
}
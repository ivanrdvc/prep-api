namespace PrepApi.Data;

public class Entity
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
}
namespace PrepApi.Shared.Dtos;

public record StepDto
{
    public required string Description { get; init; }
    public required short Order { get; init; }
}
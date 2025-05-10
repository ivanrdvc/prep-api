using PrepApi.Data;

namespace PrepApi.Contracts;

public record TagDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }

    public static TagDto FromTag(Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name
        };
    }
}
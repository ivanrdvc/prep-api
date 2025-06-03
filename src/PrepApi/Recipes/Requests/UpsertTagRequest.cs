using FluentValidation;

namespace PrepApi.Recipes.Requests;

public record UpsertTagRequest
{
    public required string Name { get; init; }
}

public class UpsertTagRequestValidator : AbstractValidator<UpsertTagRequest>
{
    public UpsertTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);
    }
}
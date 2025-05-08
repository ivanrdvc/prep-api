using FluentValidation;

using PrepApi.Data;

namespace PrepApi.Contracts;

public record CreateRecipeRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int PrepTimeMinutes { get; init; }
    public required int CookTimeMinutes { get; init; }
    public string? Yield { get; init; }
    public required List<StepDto> Steps { get; init; }
    public required List<RecipeIngredientInputDto> Ingredients { get; init; }
}

public record RecipeIngredientInputDto
{
    public required Guid IngredientId { get; init; }
    public required decimal Quantity { get; init; }
    public required Unit Unit { get; init; }
}

public class CreateRecipeRequestValidator : AbstractValidator<CreateRecipeRequest>
{
    public CreateRecipeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Recipe name is required.")
            .MaximumLength(256);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Preparation time cannot be negative.");

        RuleFor(x => x.CookTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Cook time cannot be negative.");

        RuleFor(x => x.Yield)
            .MaximumLength(256);

        RuleFor(x => x.Steps)
            .NotEmpty().WithMessage("At least one step is required.");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Description)
                .NotEmpty()
                .MaximumLength(256);
        });

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("At least one ingredient is required.");

        RuleForEach(x => x.Ingredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.IngredientId).NotEmpty();
            ingredient.RuleFor(i => i.Quantity).GreaterThan(0);
            ingredient.RuleFor(i => i.Unit).IsInEnum();
        });
    }
}
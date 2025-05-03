using FluentValidation;

namespace PrepApi.Contracts;

public record UpdateRecipeRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int PrepTime { get; init; }
    public required int CookTime { get; init; }
    public string? Yield { get; init; }
    public required List<StepDto> Steps { get; init; }
    public required List<RecipeIngredientInputDto> Ingredients { get; init; }
}

public class UpdateRecipeRequestValidator : AbstractValidator<UpdateRecipeRequest>
{
    public UpdateRecipeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Recipe name is required.")
            .MaximumLength(256);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.PrepTime)
            .GreaterThanOrEqualTo(0).WithMessage("Preparation time cannot be negative.");

        RuleFor(x => x.CookTime)
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
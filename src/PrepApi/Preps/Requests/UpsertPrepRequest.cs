using FluentValidation;

using PrepApi.Shared.Dtos;

namespace PrepApi.Preps.Requests;

public record UpsertPrepRequest
{
    public required Guid RecipeId { get; init; }
    public string? SummaryNotes { get; init; }
    public required int PrepTimeMinutes { get; init; }
    public required int CookTimeMinutes { get; init; }
    public required List<StepDto> Steps { get; init; } = [];
    public required List<PrepIngredientInputDto> PrepIngredients { get; init; } = [];
}

public class UpsertPrepRequestValidator : AbstractValidator<UpsertPrepRequest>
{
    public UpsertPrepRequestValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty().WithMessage("Recipe ID is required.");

        RuleFor(x => x.SummaryNotes)
            .MaximumLength(2000);

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Preparation time cannot be negative.");

        RuleFor(x => x.CookTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Cook time cannot be negative.");

        RuleFor(x => x.Steps)
            .NotEmpty().WithMessage("At least one step is required.");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Description)
                .NotEmpty()
                .MaximumLength(256);
        });

        RuleFor(x => x.PrepIngredients)
            .NotEmpty().WithMessage("At least one ingredient is required.");

        RuleForEach(x => x.PrepIngredients).ChildRules(ingredient =>
        {
            ingredient.RuleFor(i => i.IngredientId).NotEmpty();
            ingredient.RuleFor(i => i.Quantity).GreaterThan(0);
            ingredient.RuleFor(i => i.Unit).IsInEnum();
            ingredient.RuleFor(i => i.Notes).MaximumLength(500);
        });
    }
}
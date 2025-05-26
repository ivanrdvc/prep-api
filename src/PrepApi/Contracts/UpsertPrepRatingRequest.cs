using FluentValidation;

namespace PrepApi.Contracts;

public class UpsertPrepRatingRequest
{
    public bool Liked { get; set; }
    public int OverallRating { get; set; }
    public Dictionary<string, int> Dimensions { get; set; } = new();
    public string? WhatWorkedWell { get; set; }
    public string? WhatToChange { get; set; }
    public string? AdditionalNotes { get; set; }
}

public class PrepRatingDto
{
    public Guid Id { get; set; }
    public Guid PrepId { get; set; }
    public required string UserId { get; set; }
    public bool Liked { get; set; }
    public int OverallRating { get; set; }
    public Dictionary<string, int> Dimensions { get; set; } = new();
    public string? WhatWorkedWell { get; set; }
    public string? WhatToChange { get; set; }
    public string? AdditionalNotes { get; set; }
    public DateTimeOffset RatedAt { get; set; }
}

public class UpsertPrepRatingRequestValidator : AbstractValidator<UpsertPrepRatingRequest>
{
    public UpsertPrepRatingRequestValidator()
    {
        RuleFor(x => x.OverallRating)
            .InclusiveBetween(1, 5)
            .WithMessage("Overall rating must be between 1 and 5.");

        RuleFor(x => x.WhatWorkedWell)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.WhatWorkedWell));

        RuleFor(x => x.WhatToChange)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.WhatToChange));

        RuleFor(x => x.AdditionalNotes)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.AdditionalNotes));
    }
}
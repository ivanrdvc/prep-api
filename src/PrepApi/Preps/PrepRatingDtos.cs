namespace PrepApi.Preps;

public class PrepRatingDto
{
    public Guid Id { get; set; }
    public Guid PrepId { get; set; }
    public Guid UserId { get; set; }
    public bool Liked { get; set; }
    public int OverallRating { get; set; }
    public Dictionary<string, int> Dimensions { get; set; } = new();
    public string? WhatWorkedWell { get; set; }
    public string? WhatToChange { get; set; }
    public string? AdditionalNotes { get; set; }
    public DateTimeOffset RatedAt { get; set; }
}
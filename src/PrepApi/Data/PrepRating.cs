namespace PrepApi.Data;

public class PrepRating : Entity
{
    public Guid PrepId { get; set; }
    public Prep Prep { get; set; } = null!;
    public required string UserId { get; set; }
    public bool Liked { get; set; }
    public int OverallRating { get; set; } = 1;
    public int? TasteRating { get; set; }
    public int? TextureRating { get; set; }
    public int? AppearanceRating { get; set; }
    public string? WhatWorkedWell { get; set; }
    public string? WhatToChange { get; set; }
    public string? AdditionalNotes { get; set; }
}
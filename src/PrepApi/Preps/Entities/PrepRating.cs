using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

using PrepApi.Data;
using PrepApi.Users;

namespace PrepApi.Preps.Entities;

public class PrepRating : Entity
{
    public Guid PrepId { get; set; }
    public Prep Prep { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool Liked { get; set; }
    public int OverallRating { get; set; } = 1;
    public string? DimensionsJson { get; set; }
    public string? WhatWorkedWell { get; set; }
    public string? WhatToChange { get; set; }
    public string? AdditionalNotes { get; set; }

    [NotMapped]
    public Dictionary<string, int> Dimensions
    {
        get => string.IsNullOrEmpty(DimensionsJson)
            ? new()
            : JsonSerializer.Deserialize<Dictionary<string, int>>(DimensionsJson) ?? new();
        set => DimensionsJson = value.Count > 0 ? JsonSerializer.Serialize(value) : null;
    }
}
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PrepApi.Data;

public class RecipeInsight : Entity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public double AverageOverallRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalPreparations { get; set; }
    public string? DimensionAveragesJson { get; set; }
    public double? RatingTrend { get; set; }

    [NotMapped]
    public Dictionary<string, double> DimensionAverages
    {
        get => string.IsNullOrEmpty(DimensionAveragesJson)
            ? new()
            : JsonSerializer.Deserialize<Dictionary<string, double>>(DimensionAveragesJson) ?? new();
        set => DimensionAveragesJson = value.Count > 0 ? JsonSerializer.Serialize(value) : null;
    }
}
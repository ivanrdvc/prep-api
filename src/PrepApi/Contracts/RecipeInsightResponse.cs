namespace PrepApi.Contracts;

public record RecipeInsightResponse
{
    public Guid RecipeId { get; init; }
    public double AverageOverallRating { get; init; }
    public int TotalRatings { get; init; }
    public int TotalPreparations { get; init; }
    public Dictionary<string, double> DimensionAverages { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}
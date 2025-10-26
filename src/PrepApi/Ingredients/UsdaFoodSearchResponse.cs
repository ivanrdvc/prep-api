using System.Text.Json.Serialization;

namespace PrepApi.Ingredients;

public class UsdaFoodSearchResponse
{
    [JsonPropertyName("foods")]
    public List<UsdaFoodItem> Foods { get; set; } = new();
}

public class UsdaFoodItem
{
    [JsonPropertyName("fdcId")]
    public int FdcId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }

    [JsonPropertyName("foodCategory")]
    public string? FoodCategory { get; set; }

    [JsonPropertyName("publicationDate")]
    public string? PublicationDate { get; set; }

    [JsonPropertyName("foodNutrients")]
    public List<UsdaFoodNutrient>? FoodNutrients { get; set; }
}

public class UsdaFoodNutrient
{
    [JsonPropertyName("nutrientName")]
    public string? NutrientName { get; set; }

    [JsonPropertyName("value")]
    public double? Value { get; set; }

    [JsonPropertyName("unitName")]
    public string? UnitName { get; set; }
}
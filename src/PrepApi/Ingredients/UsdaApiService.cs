namespace PrepApi.Ingredients;

public class UsdaApiService(HttpClient httpClient, IConfiguration config, ILogger<UsdaApiService> logger)
{
    private readonly string _apiKey = config["UsdaApi:ApiKey"] ?? throw new ArgumentNullException("UsdaApi:ApiKey");

    public async Task<UsdaFoodSearchResponse?> SearchIngredientsAsync(string query)
    {
        var url = $"https://api.nal.usda.gov/fdc/v1/foods/search?query={Uri.EscapeDataString(query)}&api_key={_apiKey}";
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<UsdaFoodSearchResponse>();
    }

    public async Task<UsdaFoodSearchResponse?> SearchFoundationFoodsAsync(string query)
    {
        var searchRequest = new
        {
            query,
            dataType = new[] { "Foundation" },
            pageSize = 50
        };

        var response = await httpClient.PostAsJsonAsync(
            $"https://api.nal.usda.gov/fdc/v1/foods/search?api_key={_apiKey}",
            searchRequest);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UsdaFoodSearchResponse>();
        }

        logger.LogWarning("USDA API request failed with status: {StatusCode}", response.StatusCode);
        return null;
    }
}
namespace PrepApi.Ingredients.Requests;

// ReSharper disable once ClassNeverInstantiated.Global
public record GetIngredientsRequest(
    int Skip = 0,
    int Take = 100,
    string? Name = null);
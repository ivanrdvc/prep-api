using PrepApi.Contracts;

namespace PrepApi.Data;

public class Prep : Entity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public required string UserId { get; set; }
    public string? SummaryNotes { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public List<PrepIngredient> PrepIngredients { get; set; } = [];
    public required string StepsJson { get; set; }
    public Guid? CreatedNewRecipeId { get; set; }
    public Recipe? CreatedNewRecipe { get; set; }

    public static List<PrepIngredient> CreatePrepIngredients(
        IEnumerable<PrepIngredientInputDto> prepIngredients,
        Recipe baseRecipe)
    {
        var baseRecipeIngredientsLookup = baseRecipe.RecipeIngredients.ToDictionary(ri => ri.IngredientId, ri => ri);
        var prepIngredientsToSave = new List<PrepIngredient>();

        foreach (var ingredientInput in prepIngredients)
        {
            var prepIngredient = new PrepIngredient
            {
                IngredientId = ingredientInput.IngredientId,
                Quantity = ingredientInput.Quantity,
                Unit = ingredientInput.Unit,
                Notes = ingredientInput.Notes,
            };

            if (baseRecipeIngredientsLookup.TryGetValue(ingredientInput.IngredientId, out var baseRecipeIngredient))
            {
                // Found a match determine if kept or modified
                var quantityMatch = baseRecipeIngredient.Quantity == ingredientInput.Quantity;
                var unitMatch = baseRecipeIngredient.Unit == ingredientInput.Unit;

                prepIngredient.Status = (quantityMatch && unitMatch)
                    ? PrepIngredientStatus.Kept
                    : PrepIngredientStatus.Modified;
            }
            else
            {
                // No match found this ingredient was added
                prepIngredient.Status = PrepIngredientStatus.Added;
            }

            prepIngredientsToSave.Add(prepIngredient);
        }

        return prepIngredientsToSave;
    }
}

public class PrepIngredient
{
    public Guid Id { get; set; }
    public Guid PrepId { get; set; }
    public Prep? Prep { get; set; }
    public Guid IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public decimal Quantity { get; set; }
    public Unit Unit { get; set; }
    public string? Notes { get; set; }
    public PrepIngredientStatus Status { get; set; }
}

public enum PrepIngredientStatus
{
    Added = 1,
    Kept = 2,
    Modified = 3
}
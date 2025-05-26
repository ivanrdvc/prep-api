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
    public ICollection<PrepIngredient> PrepIngredients { get; set; } = [];
    public required string StepsJson { get; set; }
    public Guid? CreatedNewRecipeId { get; set; }
    public Recipe? CreatedNewRecipe { get; set; }
    public ICollection<PrepRating> Ratings { get; set; } = [];

    public static List<PrepIngredient> CreatePrepIngredients(
        IEnumerable<PrepIngredientInputDto> inputs,
        Recipe recipe)
    {
        var recipeLookup = recipe.RecipeIngredients.ToDictionary(ri => ri.IngredientId, ri => ri);

        return inputs.Select(input =>
        {
            var prep = new PrepIngredient
            {
                IngredientId = input.IngredientId,
                Quantity = input.Quantity,
                Unit = input.Unit,
                Notes = input.Notes,
            };

            if (recipeLookup.TryGetValue(input.IngredientId, out var baseIngredient))
            {
                bool qtyMatch = baseIngredient.Quantity == input.Quantity;
                bool unitMatch = baseIngredient.Unit == input.Unit;

                prep.Status = (qtyMatch && unitMatch)
                    ? PrepIngredientStatus.Kept
                    : PrepIngredientStatus.Modified;
            }
            else
            {
                prep.Status = PrepIngredientStatus.Added;
            }

            return prep;
        }).ToList();
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
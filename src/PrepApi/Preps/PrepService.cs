using PrepApi.Data;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;

namespace PrepApi.Preps;

public class PrepService
{
    /// <summary>
    /// Gets a summary of changes made compared to the recipe.
    /// </summary>
    public string GetChangeSummary(Prep prep, Recipe recipe, Dictionary<Guid, Ingredient> ingredients)
    {
        var changes = new List<string>();

        var ingredientChanges = AnalyzeIngredientChanges(prep.PrepIngredients, recipe.RecipeIngredients, ingredients);
        changes.AddRange(ingredientChanges);

        var timingChange = AnalyzeTimingChanges(prep, recipe);
        if (!string.IsNullOrEmpty(timingChange))
        {
            changes.Add(timingChange);
        }

        return changes.Count != 0
            ? "Changes made:\n" + string.Join("\n", changes.Select(c => $"- {c}"))
            : "No changes made from original recipe.";
    }

    /// <summary>
    /// Creates PrepIngredients from input DTOs, determining their status compared to the original recipe.
    /// </summary>
    public List<PrepIngredient> CreateIngredients(IEnumerable<PrepIngredientInputDto> inputs, Recipe recipe)
    {
        var recipeLookup = recipe.RecipeIngredients.ToDictionary(ri => ri.IngredientId, ri => ri);

        return inputs.Select(input =>
        {
            var prepIngredient = new PrepIngredient
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

                prepIngredient.Status = (qtyMatch && unitMatch) ? PrepIngredientStatus.Kept : PrepIngredientStatus.Modified;
            }
            else
            {
                prepIngredient.Status = PrepIngredientStatus.Added;
            }

            return prepIngredient;
        }).ToList();
    }

    private List<string> AnalyzeIngredientChanges(
        ICollection<PrepIngredient> prepIngredients,
        ICollection<RecipeIngredient> recipeIngredients,
        Dictionary<Guid, Ingredient> ingredients)
    {
        var changes = new List<string>();
        var recipeIngredientsDict = recipeIngredients.ToDictionary(ri => ri.IngredientId, ri => ri);
        var usedRecipeIngredients = new HashSet<Guid>();

        foreach (var prepIngredient in prepIngredients)
        {
            if (recipeIngredientsDict.TryGetValue(prepIngredient.IngredientId, out var originalIngredient))
            {
                usedRecipeIngredients.Add(prepIngredient.IngredientId);

                if (prepIngredient.Status != PrepIngredientStatus.Modified)
                {
                    continue;
                }

                if (originalIngredient.Quantity != prepIngredient.Quantity || originalIngredient.Unit != prepIngredient.Unit)
                {
                    var originalAmount = $"{originalIngredient.Quantity} {GetUnitAbbreviation(originalIngredient.Unit)}";
                    var newAmount = $"{prepIngredient.Quantity} {GetUnitAbbreviation(prepIngredient.Unit)}";

                    var ingredientName = ingredients.TryGetValue(prepIngredient.IngredientId, out var ingredient)
                        ? ingredient.Name
                        : "ingredient";

                    changes.Add($"Modified: {ingredientName} ({originalAmount} → {newAmount})");
                }
            }
            else if (prepIngredient.Status == PrepIngredientStatus.Added)
            {
                var ingredientName = ingredients.TryGetValue(prepIngredient.IngredientId, out var ingredient)
                    ? ingredient.Name
                    : "ingredient";
                changes.Add($"Addition: added {ingredientName}");
            }
        }

        var omittedIngredients = recipeIngredientsDict.Keys.Except(usedRecipeIngredients);
        foreach (var omittedId in omittedIngredients)
        {
            var ingredientName = ingredients.TryGetValue(omittedId, out var ingredient)
                ? ingredient.Name
                : "ingredient";
            changes.Add($"Omission: removed {ingredientName}");
        }

        return changes;
    }

    private static string AnalyzeTimingChanges(Prep prep, Recipe originalRecipe)
    {
        var timingParts = new List<string>();

        if (prep.PrepTimeMinutes.HasValue && prep.PrepTimeMinutes != originalRecipe.PrepTimeMinutes)
        {
            var difference = prep.PrepTimeMinutes.Value - originalRecipe.PrepTimeMinutes;
            var action = difference > 0 ? "increased" : "decreased";
            timingParts.Add($"prep time {action} by {Math.Abs(difference)} min");
        }

        if (prep.CookTimeMinutes.HasValue && prep.CookTimeMinutes != originalRecipe.CookTimeMinutes)
        {
            var difference = prep.CookTimeMinutes.Value - originalRecipe.CookTimeMinutes;
            var action = difference > 0 ? "increased" : "decreased";
            timingParts.Add($"cook time {action} by {Math.Abs(difference)} min");
        }

        return timingParts.Any() ? $"Timing: {string.Join(", ", timingParts)}" : string.Empty;
    }

    private static string GetUnitAbbreviation(Unit unit)
    {
        return unit switch
        {
            Unit.Whole => "whole",
            Unit.Gram => "g",
            Unit.Kilogram => "kg",
            Unit.Milliliter => "ml",
            _ => unit.ToString().ToLower()
        };
    }
}
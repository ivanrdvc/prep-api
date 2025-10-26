using PrepApi.Data;
using PrepApi.Ingredients;

namespace PrepApi.Preps.Entities;

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
using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Tests.Unit;

public class PrepTests
{
    private readonly Guid _recipeId = Guid.NewGuid();
    private readonly Guid _ingredientId1 = Guid.NewGuid();
    private readonly Guid _ingredientId2 = Guid.NewGuid();
    private readonly Guid _ingredientId3 = Guid.NewGuid();
    private readonly string _userId = "test-user";

    private readonly List<StepDto> _defaultSteps = [new() { Description = "Default step", Order = 1 }];

    [Fact]
    public void CreateWithVariations_ShouldSetPrepPropertiesCorrectly()
    {
        var baseRecipe = CreateBaseRecipe([]);
        var request = new CreatePrepRequest
        {
            RecipeId = _recipeId,
            SummaryNotes = "Test notes",
            PrepTimeMinutes = 15,
            CookTimeMinutes = 25,
            PrepIngredients = [],
            Steps = _defaultSteps
        };

        var prep = Prep.CreateWithVariations(request, baseRecipe, _userId);

        Assert.Equal(_recipeId, prep.RecipeId);
        Assert.Equal(_userId, prep.UserId);
        Assert.Equal("Test notes", prep.SummaryNotes);
        Assert.Equal(15, prep.PrepTimeMinutes);
        Assert.Equal(25, prep.CookTimeMinutes);
        Assert.Contains("Default step", prep.StepsJson);
    }

    [Fact]
    public void CreateWithVariations_ShouldMarkIngredientsAsKept()
    {
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var request = new CreatePrepRequest
        {
            RecipeId = _recipeId,
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            PrepIngredients =
            [
                new() { IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
                new() { IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
            ],
            Steps = _defaultSteps
        };

        var prep = Prep.CreateWithVariations(request, baseRecipe, _userId);

        Assert.Equal(2, prep.PrepIngredients.Count);
        Assert.All(prep.PrepIngredients, pi => Assert.Equal(PrepIngredientStatus.Kept, pi.Status));
        Assert.All(prep.PrepIngredients, pi => Assert.Equal(_recipeId, pi.BasedOnRecipeId));
        Assert.Contains(prep.PrepIngredients, pi => pi.BasedOnIngredientId == _ingredientId1);
        Assert.Contains(prep.PrepIngredients, pi => pi.BasedOnIngredientId == _ingredientId2);
    }

    [Fact]
    public void CreateWithVariations_ShouldMarkIngredientsAsModified()
    {
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var request = new CreatePrepRequest
        {
            RecipeId = _recipeId,
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            PrepIngredients =
            [
                new() { IngredientId = _ingredientId1, Quantity = 150, Unit = Data.Unit.Gram },
                new() { IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Kilogram }
            ],
            Steps = _defaultSteps
        };

        var prep = Prep.CreateWithVariations(request, baseRecipe, _userId);

        Assert.Equal(2, prep.PrepIngredients.Count);
        Assert.All(prep.PrepIngredients, pi => Assert.Equal(PrepIngredientStatus.Modified, pi.Status));
        Assert.All(prep.PrepIngredients, pi => Assert.Equal(_recipeId, pi.BasedOnRecipeId));
        Assert.Contains(prep.PrepIngredients, pi => pi.BasedOnIngredientId == _ingredientId1);
        Assert.Contains(prep.PrepIngredients, pi => pi.BasedOnIngredientId == _ingredientId2);
    }

    [Fact]
    public void CreateWithVariations_ShouldMarkIngredientsAsAdded()
    {
        var baseRecipe = CreateBaseRecipe([]);

        var request = new CreatePrepRequest
        {
            RecipeId = _recipeId,
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            PrepIngredients =
            [
                new() { IngredientId = _ingredientId1, Quantity = 50, Unit = Data.Unit.Milliliter },
                new() { IngredientId = _ingredientId2, Quantity = 1, Unit = Data.Unit.Whole }
            ],
            Steps = _defaultSteps
        };

        var prep = Prep.CreateWithVariations(request, baseRecipe, _userId);

        Assert.Equal(2, prep.PrepIngredients.Count);
        Assert.All(prep.PrepIngredients, pi => Assert.Equal(PrepIngredientStatus.Added, pi.Status));
        Assert.All(prep.PrepIngredients, pi => Assert.Null(pi.BasedOnRecipeId));
        Assert.All(prep.PrepIngredients, pi => Assert.Null(pi.BasedOnIngredientId));
    }

    [Fact]
    public void CreateWithVariations_ShouldHandleMixedIngredientStatuses()
    {
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var request = new CreatePrepRequest
        {
            RecipeId = _recipeId,
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            PrepIngredients =
            [
                new() { IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
                new() { IngredientId = _ingredientId2, Quantity = 3, Unit = Data.Unit.Whole },
                new() { IngredientId = _ingredientId3, Quantity = 500, Unit = Data.Unit.Milliliter }
            ],
            Steps = _defaultSteps
        };

        var prep = Prep.CreateWithVariations(request, baseRecipe, _userId);

        Assert.Equal(3, prep.PrepIngredients.Count);

        var keptIngredient = prep.PrepIngredients.Single(pi => pi.IngredientId == _ingredientId1);
        Assert.Equal(PrepIngredientStatus.Kept, keptIngredient.Status);
        Assert.Equal(_recipeId, keptIngredient.BasedOnRecipeId);
        Assert.Equal(_ingredientId1, keptIngredient.BasedOnIngredientId);

        var modifiedIngredient = prep.PrepIngredients.Single(pi => pi.IngredientId == _ingredientId2);
        Assert.Equal(PrepIngredientStatus.Modified, modifiedIngredient.Status);
        Assert.Equal(_recipeId, modifiedIngredient.BasedOnRecipeId);
        Assert.Equal(_ingredientId2, modifiedIngredient.BasedOnIngredientId);

        var addedIngredient = prep.PrepIngredients.Single(pi => pi.IngredientId == _ingredientId3);
        Assert.Equal(PrepIngredientStatus.Added, addedIngredient.Status);
        Assert.Null(addedIngredient.BasedOnRecipeId);
        Assert.Null(addedIngredient.BasedOnIngredientId);
    }

    [Fact]
    public void CreateWithVariations_ShouldHandleEmptyRequestIngredients()
    {
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var request = new CreatePrepRequest
        {
            RecipeId = _recipeId,
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            PrepIngredients = [],
            Steps = _defaultSteps
        };

        var prep = Prep.CreateWithVariations(request, baseRecipe, _userId);

        Assert.Empty(prep.PrepIngredients);
    }

    private Recipe CreateBaseRecipe(List<RecipeIngredient> ingredients)
    {
        return new Recipe
        {
            Id = _recipeId,
            Name = "Base Recipe",
            UserId = _userId,
            Description = "Base Description",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            StepsJson = System.Text.Json.JsonSerializer.Serialize(_defaultSteps),
            RecipeIngredients = ingredients
        };
    }
}
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
    public void CreatePrep_ShouldSetPrepPropertiesCorrectly()
    {
        // Arrange
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

        // Act
        var prep = new Prep
        {
            RecipeId = _recipeId,
            UserId = _userId,
            SummaryNotes = request.SummaryNotes,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            StepsJson = System.Text.Json.JsonSerializer.Serialize(request.Steps)
        };

        prep.PrepIngredients = Prep.CreatePrepIngredients(request.PrepIngredients, baseRecipe);

        // Assert
        Assert.Equal(_recipeId, prep.RecipeId);
        Assert.Equal(_userId, prep.UserId);
        Assert.Equal("Test notes", prep.SummaryNotes);
        Assert.Equal(15, prep.PrepTimeMinutes);
        Assert.Equal(25, prep.CookTimeMinutes);
        Assert.Contains("Default step", prep.StepsJson);
    }

    [Fact]
    public void CreatePrepIngredients_ShouldMarkIngredientsAsKept()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Kept, pi.Status));
    }

    [Fact]
    public void CreatePrepIngredients_ShouldMarkIngredientsAsModified()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 150, Unit = Data.Unit.Gram },
            new() { IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Kilogram }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Modified, pi.Status));
    }

    [Fact]
    public void CreatePrepIngredients_ShouldMarkIngredientsAsAdded()
    {
        // Arrange
        var baseRecipe = CreateBaseRecipe([]);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 50, Unit = Data.Unit.Milliliter },
            new() { IngredientId = _ingredientId2, Quantity = 1, Unit = Data.Unit.Whole }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Added, pi.Status));
    }

    [Fact]
    public void CreatePrepIngredients_ShouldHandleMixedIngredientStatuses()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { IngredientId = _ingredientId2, Quantity = 3, Unit = Data.Unit.Whole },
            new() { IngredientId = _ingredientId3, Quantity = 500, Unit = Data.Unit.Milliliter }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(3, result.Count);

        var keptIngredient = result.Single(pi => pi.IngredientId == _ingredientId1);
        Assert.Equal(PrepIngredientStatus.Kept, keptIngredient.Status);

        var modifiedIngredient = result.Single(pi => pi.IngredientId == _ingredientId2);
        Assert.Equal(PrepIngredientStatus.Modified, modifiedIngredient.Status);
        
        var addedIngredient = result.Single(pi => pi.IngredientId == _ingredientId3);
        Assert.Equal(PrepIngredientStatus.Added, addedIngredient.Status);
    }

    [Fact]
    public void CreatePrepIngredients_ShouldHandleEmptyRequestIngredients()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>();

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Empty(result);
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
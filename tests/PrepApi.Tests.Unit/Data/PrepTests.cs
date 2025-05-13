using PrepApi.Contracts;
using PrepApi.Data;

namespace PrepApi.Tests.Unit.Data;

public class PrepTests
{
    private readonly Guid _recipeId = Guid.NewGuid();
    private readonly Guid _ingredientId1 = Guid.NewGuid();
    private readonly Guid _ingredientId2 = Guid.NewGuid();
    private readonly Guid _ingredientId3 = Guid.NewGuid();
    private const string UserId = "test-user-id";

    private readonly List<StepDto> _defaultSteps = [new() { Description = "Default step", Order = 1 }];


    [Fact]
    public void CreatePrepIngredients_WhenIngredientsMatchBaseRecipe_ShouldMarkAllAsKept()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = PrepApi.Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = PrepApi.Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 100, Unit = PrepApi.Data.Unit.Gram },
            new() { IngredientId = _ingredientId2, Quantity = 2, Unit = PrepApi.Data.Unit.Whole }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Kept, pi.Status));
    }

    [Fact]
    public void CreatePrepIngredients_WhenIngredientsQuantityOrUnitDiffer_ShouldMarkAllAsModified()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = PrepApi.Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = PrepApi.Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 150, Unit = PrepApi.Data.Unit.Gram },
            new() { IngredientId = _ingredientId2, Quantity = 2, Unit = PrepApi.Data.Unit.Kilogram }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Modified, pi.Status));
    }

    [Fact]
    public void CreatePrepIngredients_WhenIngredientsNotInBaseRecipe_ShouldMarkAllAsAdded()
    {
        // Arrange
        var baseRecipe = CreateBaseRecipe([]);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 50, Unit = PrepApi.Data.Unit.Milliliter },
            new() { IngredientId = _ingredientId2, Quantity = 1, Unit = PrepApi.Data.Unit.Whole }
        };

        // Act
        var result = Prep.CreatePrepIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Added, pi.Status));
    }

    [Fact]
    public void CreatePrepIngredients_WithMixedIngredientChanges_ShouldAssignCorrectStatusToEach()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = PrepApi.Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = PrepApi.Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 100, Unit = PrepApi.Data.Unit.Gram },
            new() { IngredientId = _ingredientId2, Quantity = 3, Unit = PrepApi.Data.Unit.Whole },
            new() { IngredientId = _ingredientId3, Quantity = 500, Unit = PrepApi.Data.Unit.Milliliter }
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
    public void CreatePrepIngredients_WithEmptyIngredientsList_ShouldReturnEmptyCollection()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = PrepApi.Data.Unit.Gram }
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
            UserId = UserId,
            Description = "Base Description",
            PrepTimeMinutes = 10,
            CookTimeMinutes = 20,
            StepsJson = System.Text.Json.JsonSerializer.Serialize(_defaultSteps),
            RecipeIngredients = ingredients
        };
    }
}
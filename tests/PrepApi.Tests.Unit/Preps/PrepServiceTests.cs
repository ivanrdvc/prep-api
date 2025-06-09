using PrepApi.Data;
using PrepApi.Preps;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Dtos;

namespace PrepApi.Tests.Unit.Preps;

public class PrepServiceTests
{
    private readonly PrepService _prepService = new();
    private readonly Guid _recipeId = Guid.NewGuid();
    private readonly Guid _ingredientId1 = Guid.NewGuid();
    private readonly Guid _ingredientId2 = Guid.NewGuid();
    private readonly Guid _ingredientId3 = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private readonly List<StepDto> _defaultSteps = [new() { Description = "Default step", Order = 1 }];

    [Fact]
    public void CreateIngredients_WhenIngredientsMatchBaseRecipe_ShouldMarkAllAsKept()
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
        var result = _prepService.CreateIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Kept, pi.Status));
    }

    [Fact]
    public void CreateIngredients_WhenIngredientsQuantityOrUnitDiffer_ShouldMarkAllAsModified()
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
        var result = _prepService.CreateIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Modified, pi.Status));
    }

    [Fact]
    public void CreateIngredients_WhenIngredientsNotInBaseRecipe_ShouldMarkAllAsAdded()
    {
        // Arrange
        var baseRecipe = CreateBaseRecipe([]);

        var prepIngredients = new List<PrepIngredientInputDto>
        {
            new() { IngredientId = _ingredientId1, Quantity = 50, Unit = Data.Unit.Milliliter },
            new() { IngredientId = _ingredientId2, Quantity = 1, Unit = Data.Unit.Whole }
        };

        // Act
        var result = _prepService.CreateIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, pi => Assert.Equal(PrepIngredientStatus.Added, pi.Status));
    }

    [Fact]
    public void CreateIngredients_WithMixedIngredientChanges_ShouldAssignCorrectStatusToEach()
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
        var result = _prepService.CreateIngredients(prepIngredients, baseRecipe);

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
    public void CreateIngredients_WithEmptyIngredientsList_ShouldReturnEmptyCollection()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredientInputDto>();

        // Act
        var result = _prepService.CreateIngredients(prepIngredients, baseRecipe);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetChangeSummary_WithNoChanges_ShouldReturnNoChangesMessage()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredient>
        {
            new()
            {
                IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram,
                Status = PrepIngredientStatus.Kept
            }
        };

        var prep = new Prep
        {
            PrepIngredients = prepIngredients,
            PrepTimeMinutes = baseRecipe.PrepTimeMinutes,
            CookTimeMinutes = baseRecipe.CookTimeMinutes,
            UserId = _userId,
            StepsJson = "[]"
        };

        var ingredients = new Dictionary<Guid, Ingredient>
        {
            { _ingredientId1, new() { Id = _ingredientId1, Name = "Test Ingredient" } }
        };

        // Act
        var result = _prepService.GetChangeSummary(prep, baseRecipe, ingredients);

        // Assert
        Assert.Equal("No changes made from original recipe.", result);
    }

    [Fact]
    public void GetChangeSummary_WithIngredientChanges_ShouldReturnCorrectSummary()
    {
        // Arrange
        var baseIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = _recipeId, IngredientId = _ingredientId1, Quantity = 100, Unit = Data.Unit.Gram },
            new() { RecipeId = _recipeId, IngredientId = _ingredientId2, Quantity = 2, Unit = Data.Unit.Whole }
        };
        var baseRecipe = CreateBaseRecipe(baseIngredients);

        var prepIngredients = new List<PrepIngredient>
        {
            new()
            {
                IngredientId = _ingredientId1, Quantity = 150, Unit = Data.Unit.Gram,
                Status = PrepIngredientStatus.Modified
            },
            new()
            {
                IngredientId = _ingredientId3, Quantity = 1, Unit = Data.Unit.Whole,
                Status = PrepIngredientStatus.Added
            }
        };

        var prep = new Prep
        {
            PrepIngredients = prepIngredients,
            PrepTimeMinutes = baseRecipe.PrepTimeMinutes,
            CookTimeMinutes = baseRecipe.CookTimeMinutes,
            UserId = _userId,
            StepsJson = "[]"
        };

        var ingredients = new Dictionary<Guid, Ingredient>
        {
            { _ingredientId1, new() { Id = _ingredientId1, Name = "Flour" } },
            { _ingredientId2, new() { Id = _ingredientId2, Name = "Eggs" } },
            { _ingredientId3, new() { Id = _ingredientId3, Name = "Salt" } }
        };

        // Act
        var result = _prepService.GetChangeSummary(prep, baseRecipe, ingredients);

        // Assert
        Assert.Contains("Changes made:", result);
        Assert.Contains("Modified: Flour (100 g → 150 g)", result);
        Assert.Contains("Addition: added Salt", result);
        Assert.Contains("Omission: removed Eggs", result);
    }

    [Fact]
    public void GetChangeSummary_WithTimingChanges_ShouldIncludeTimingInformation()
    {
        // Arrange
        var baseRecipe = CreateBaseRecipe([]);

        var prep = new Prep
        {
            UserId = _userId,
            StepsJson = "[]",
            PrepIngredients = [],
            PrepTimeMinutes = 15,
            CookTimeMinutes = 25
        };

        var ingredients = new Dictionary<Guid, Ingredient>();

        // Act
        var result = _prepService.GetChangeSummary(prep, baseRecipe, ingredients);

        // Assert
        Assert.Contains("Changes made:", result);
        Assert.Contains("Timing: prep time increased by 5 min, cook time increased by 5 min", result);
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
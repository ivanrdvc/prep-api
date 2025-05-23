﻿using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;

namespace PrepApi.Data;

public class PrepDb(DbContextOptions<PrepDb> options, IUserContext userContext) : DbContext(options)
{
    private readonly IUserContext userContext = userContext;

    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<Prep> Preps { get; set; }
    public DbSet<PrepIngredient> PrepIngredients { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<RecipeTag> RecipeTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(p => p.Name)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(p => p.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(p => p.Yield)
                .HasMaxLength(256);

            entity.Property(p => p.StepsJson)
                .IsRequired()
                .HasColumnType("jsonb");

            entity.Property(p => p.UserId)
                .IsRequired();

            entity.HasIndex(r => r.UserId);

            entity.HasOne(r => r.OriginalRecipe)
                .WithMany(r => r.Variants)
                .HasForeignKey(r => r.OriginalRecipeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.Property(r => r.IsFavoriteVariant)
                .HasDefaultValue(false);
            
            entity.HasMany<Prep>()
                .WithOne(p => p.Recipe)
                .HasForeignKey(p => p.RecipeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Prep>()
                .WithOne(p => p.CreatedNewRecipe)
                .HasForeignKey<Prep>(p => p.CreatedNewRecipeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(ri => new { ri.RecipeId, ri.IngredientId });

            entity.HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ri => ri.Ingredient)
                .WithMany()
                .HasForeignKey(ri => ri.IngredientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(ri => ri.Quantity)
                .IsRequired();

            entity.Property(ri => ri.Unit)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(t => t.UserId).IsRequired();

            entity.HasIndex(t => new { t.Name, t.UserId }).IsUnique();
        });

        modelBuilder.Entity<RecipeTag>(entity =>
        {
            entity.HasKey(rt => new { rt.RecipeId, rt.TagId });

            entity.HasOne(rt => rt.Recipe)
                .WithMany(r => r.RecipeTags)
                .HasForeignKey(rt => rt.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rt => rt.Tag)
                .WithMany(t => t.RecipeTags)
                .HasForeignKey(rt => rt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ingredient>(entity => entity.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(256));

        modelBuilder.Entity<Prep>(entity =>
        {
            entity.HasOne(p => p.Recipe)
                .WithMany()
                .HasForeignKey(p => p.RecipeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(p => p.UserId)
                .IsRequired();
            entity.HasIndex(p => p.UserId);

            entity.Property(p => p.StepsJson)
                .IsRequired()
                .HasColumnType("jsonb");

            entity.Property(p => p.SummaryNotes)
                .HasMaxLength(2000);
        });

        modelBuilder.Entity<PrepIngredient>(entity =>
        {
            entity.HasOne(pi => pi.Prep)
                .WithMany(p => p.PrepIngredients)
                .HasForeignKey(pi => pi.PrepId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.Ingredient)
                .WithMany()
                .HasForeignKey(pi => pi.IngredientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(pi => pi.Quantity)
                .IsRequired();

            entity.Property(pi => pi.Unit)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(pi => pi.Notes)
                .HasMaxLength(500);

            entity.Property(pi => pi.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.HasIndex(pi => pi.Status);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                if (context is PrepDb prepDbContext)
                {
                    if (await prepDbContext.Recipes.AnyAsync(cancellationToken))
                    {
                        return; 
                    }

                    var flourId = new Guid("11111111-1111-1111-1111-111111111111");
                    var butterId = new Guid("22222222-2222-2222-2222-222222222222");
                    var sugarId = new Guid("33333333-3333-3333-3333-333333333333");
                    var saltId = new Guid("44444444-4444-4444-4444-444444444444");

                    var sweetTagId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
                    var basicTagId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

                    var recipeId = new Guid("55555555-5555-5555-5555-555555555555");
                    var prepId = new Guid("66666666-6666-6666-6666-666666666666");

                    // Seed Ingredients
                    prepDbContext.Ingredients.AddRange(
                        new Ingredient { Id = flourId, Name = "Flour" },
                        new Ingredient { Id = butterId, Name = "Butter" },
                        new Ingredient { Id = sugarId, Name = "Sugar" },
                        new Ingredient { Id = saltId, Name = "Salt" }
                    );

                    // Seed Tags
                    prepDbContext.Tags.AddRange(
                        new Tag { Id = sweetTagId, Name = "Sweet", UserId = "SeedUser" },
                        new Tag { Id = basicTagId, Name = "Basic", UserId = "SeedUser" }
                    );

                    var recipeSteps = new List<StepDto>
                    {
                        new() { Order = 1, Description = "Mix dry ingredients." },
                        new() { Order = 2, Description = "Add wet ingredients." },
                        new() { Order = 3, Description = "Cook until done." }
                    };

                    // Seed Recipe
                    var recipe = new Recipe
                    {
                        Id = recipeId,
                        Name = "Seeded Recipe",
                        UserId = "SeedUser",
                        Description = "Basic recipe description.",
                        PrepTimeMinutes = 5,
                        CookTimeMinutes = 10,
                        Yield = "8 servings",
                        StepsJson = JsonSerializer.Serialize(recipeSteps),
                    };
                    prepDbContext.Recipes.Add(recipe);

                    // Seed RecipeIngredients
                    prepDbContext.RecipeIngredients.AddRange(
                        new RecipeIngredient
                            { RecipeId = recipeId, IngredientId = flourId, Quantity = 2, Unit = Unit.Whole },
                        new RecipeIngredient
                            { RecipeId = recipeId, IngredientId = butterId, Quantity = 3, Unit = Unit.Gram },
                        new RecipeIngredient
                            { RecipeId = recipeId, IngredientId = sugarId, Quantity = 4, Unit = Unit.Kilogram },
                        new RecipeIngredient
                            { RecipeId = recipeId, IngredientId = saltId, Quantity = 0.5m, Unit = Unit.Milliliter }
                    );

                    // Seed Recipe Tags
                    prepDbContext.RecipeTags.AddRange(
                        new RecipeTag { RecipeId = recipeId, TagId = sweetTagId },
                        new RecipeTag { RecipeId = recipeId, TagId = basicTagId }
                    );

                    // Create prep steps (slightly modified from recipe)
                    var prepSteps = new List<StepDto>
                    {
                        new() { Order = 1, Description = "Mix dry ingredients in a large bowl." },
                        new() { Order = 2, Description = "Add wet ingredients slowly while stirring." },
                        new() { Order = 3, Description = "Cook until golden brown." }
                    };

                    // Seed Prep
                    var prep = new Prep
                    {
                        Id = prepId,
                        RecipeId = recipeId,
                        UserId = "SeedUser",
                        SummaryNotes = "Made this with a bit more butter than called for.",
                        PrepTimeMinutes = 7,
                        CookTimeMinutes = 12,
                        StepsJson = JsonSerializer.Serialize(prepSteps),
                        CreatedNewRecipeId = null
                    };
                    prepDbContext.Preps.Add(prep);

                    // Seed PrepIngredients
                    prepDbContext.PrepIngredients.AddRange(
                        new PrepIngredient
                        {
                            Id = Guid.NewGuid(),
                            PrepId = prepId,
                            IngredientId = flourId,
                            Quantity = 2,
                            Unit = Unit.Whole,
                            Status = PrepIngredientStatus.Kept
                        },
                        new PrepIngredient
                        {
                            Id = Guid.NewGuid(),
                            PrepId = prepId,
                            IngredientId = butterId,
                            Quantity = 4,
                            Unit = Unit.Gram,
                            Notes = "Used more butter",
                            Status = PrepIngredientStatus.Modified
                        },
                        new PrepIngredient
                        {
                            Id = Guid.NewGuid(),
                            PrepId = prepId,
                            IngredientId = sugarId,
                            Quantity = 4,
                            Unit = Unit.Kilogram,
                            Status = PrepIngredientStatus.Kept
                        },
                        new PrepIngredient
                        {
                            Id = Guid.NewGuid(),
                            PrepId = prepId,
                            IngredientId = saltId,
                            Quantity = 0.5m,
                            Unit = Unit.Milliliter,
                            Status = PrepIngredientStatus.Kept
                        }
                    );

                    await prepDbContext.SaveChangesAsync(cancellationToken);
                }
            });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var timestamp = DateTime.UtcNow;
        var userId = userContext.UserId ?? "system";

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = timestamp;
                entry.Entity.CreatedBy = userId;
                entry.Entity.UpdatedAt = timestamp;
                entry.Entity.UpdatedBy = userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = timestamp;
                entry.Entity.UpdatedBy = userId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
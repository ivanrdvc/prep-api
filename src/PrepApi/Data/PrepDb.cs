using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;
using PrepApi.Shared.Dtos;
using PrepApi.Shared.Entities;
using PrepApi.Shared.Services;
using PrepApi.Users;

namespace PrepApi.Data;

public class PrepDb(DbContextOptions<PrepDb> options, IUserContext userContext) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<Prep> Preps { get; set; }
    public DbSet<PrepIngredient> PrepIngredients { get; set; }
    public DbSet<PrepRating> PrepRatings { get; set; }
    public DbSet<RatingDimension> RatingDimensions { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<RecipeTag> RecipeTags { get; set; }
    public DbSet<RecipeInsight> RecipeInsights { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.ExternalId)
                .HasMaxLength(256)
                .IsRequired();
            
            entity.HasIndex(u => u.ExternalId).IsUnique();
            
            entity.Property(u => u.Email).HasMaxLength(256);
            
            entity.Property(u => u.FirstName).HasMaxLength(256);

            entity.Property(u => u.LastName).HasMaxLength(256);

            entity.Property(u => u.PreferredUnits)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(r => r.Name)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(r => r.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(r => r.Yield).HasMaxLength(256);

            entity.Property(r => r.StepsJson)
                .IsRequired()
                .HasColumnType("jsonb");

            entity.Property(r => r.UserId).IsRequired();

            entity.HasIndex(r => r.UserId);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.OriginalRecipe)
                .WithMany(r => r.Variants)
                .HasForeignKey(r => r.OriginalRecipeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            entity.Property(r => r.IsFavoriteVariant).HasDefaultValue(false);

            entity.HasMany(r => r.Preps)
                .WithOne(p => p.Recipe)
                .HasForeignKey(p => p.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
                .WithMany(r => r.Preps)
                .HasForeignKey(p => p.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(p => p.UserId).IsRequired();
            entity.HasIndex(p => p.UserId);

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.CreatedNewRecipe)
                .WithMany()
                .HasForeignKey(p => p.CreatedNewRecipeId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.Property(p => p.StepsJson)
                .IsRequired()
                .HasColumnType("jsonb");

            entity.Property(p => p.ChangeSummary).HasMaxLength(2000);

            entity.Property(p => p.SummaryNotes).HasMaxLength(2000);
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

            entity.Property(pi => pi.Notes).HasMaxLength(500);

            entity.Property(pi => pi.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });

        modelBuilder.Entity<PrepRating>(entity =>
        {
            entity.HasOne(pr => pr.Prep)
                .WithMany(p => p.Ratings)
                .HasForeignKey(pr => pr.PrepId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(pr => pr.UserId).IsRequired();

            entity.HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(pr => pr.OverallRating)
                .HasDefaultValue(1)
                .IsRequired();

            entity.Property(pr => pr.Liked).IsRequired();

            entity.Property(pr => pr.DimensionsJson)
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(pr => pr.WhatWorkedWell).HasMaxLength(1000);

            entity.Property(pr => pr.WhatToChange).HasMaxLength(1000);

            entity.Property(pr => pr.AdditionalNotes).HasMaxLength(1000);

            entity.HasIndex(pr => pr.PrepId);
            entity.HasIndex(pr => pr.UserId);
            entity.HasIndex(pr => new { pr.PrepId, pr.UserId }).IsUnique();
        });

        modelBuilder.Entity<RatingDimension>(entity =>
        {
            entity.Property(d => d.Key)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(d => d.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(d => d.Description)
                .HasMaxLength(500);

            entity.Property(d => d.SortOrder)
                .HasDefaultValue(100);
        });

        modelBuilder.Entity<RecipeInsight>(entity =>
        {
            entity.HasOne(ri => ri.Recipe)
                .WithOne()
                .HasForeignKey<RecipeInsight>(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(ri => ri.AverageOverallRating)
                .HasPrecision(3, 2)
                .IsRequired();

            entity.Property(ri => ri.TotalRatings)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(ri => ri.TotalPreparations)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(ri => ri.DimensionAveragesJson)
                .HasColumnType("jsonb")
                .IsRequired(false);

            entity.Property(ri => ri.RatingTrend)
                .HasPrecision(3, 2)
                .IsRequired(false);

            entity.HasIndex(ri => ri.RecipeId).IsUnique();
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
                    
                    // Seed User
                    var seedUser = new User
                    {
                        ExternalId = "SeedUser",
                        Email = "seed@example.com",
                        FirstName = "Seed",
                        LastName = "User",
                        PreferredUnits = PreferredUnits.Metric,
                    };
                    prepDbContext.Users.Add(seedUser);
                    
                    // Seed Tags
                    prepDbContext.Tags.AddRange(
                        new Tag { Id = sweetTagId, Name = "Sweet", UserId = seedUser.Id },
                        new Tag { Id = basicTagId, Name = "Basic", UserId = seedUser.Id }
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
                        UserId = seedUser.Id,
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
                        UserId = seedUser.Id,
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

                    // Seed  Rating Dimensions
                    prepDbContext.RatingDimensions.AddRange(new RatingDimension
                        {
                            Key = "taste",
                            DisplayName = "Taste",
                            Description = "How good did the recipe taste?",
                            SortOrder = 10,
                        },
                        new RatingDimension
                        {
                            Key = "texture",
                            DisplayName = "Texture",
                            Description = "How was the texture and consistency?",
                            SortOrder = 20,
                        },
                        new RatingDimension
                        {
                            Key = "appearance",
                            DisplayName = "Appearance",
                            Description = "How did the final dish look?",
                            SortOrder = 30,
                        });

                    await prepDbContext.SaveChangesAsync(cancellationToken);
                }
            });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var timestamp = DateTimeOffset.UtcNow;;
        var userId = userContext.ExternalId ?? "system";

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
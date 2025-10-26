using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Ingredients;
using PrepApi.Preps.Entities;
using PrepApi.Recipes.Entities;
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

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(i => i.Category)
                .HasMaxLength(100)
                .IsRequired(false);

            entity.Property(i => i.UserId).IsRequired(false);
            entity.HasIndex(i => i.UserId);
            entity.HasIndex(i => new { i.Name, i.UserId }).IsUnique(); // Ensures unique names per user or shared

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

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
        optionsBuilder.UseAsyncSeeding(async (context, _, cancellationToken) =>
        {
            if (context is not PrepDb db) return;

            // TODO: Delete
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env != "Development") return;

            if (await db.Users.AnyAsync(cancellationToken)) return;

            var seedPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "seed");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var user = JsonSerializer.Deserialize<User>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "user.json"), cancellationToken), options)!;
            var ingredients = JsonSerializer.Deserialize<Ingredient[]>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "ingredients.json"), cancellationToken), options)!;
            var ratingDimensions = JsonSerializer.Deserialize<RatingDimension[]>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "rating-dimensions.json"), cancellationToken), options)!;
            var tags = JsonSerializer.Deserialize<Tag[]>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "tags.json"), cancellationToken), options)!;
            var recipe = JsonSerializer.Deserialize<Recipe>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "recipe.json"), cancellationToken), options)!;
            var recipeIngredients = JsonSerializer.Deserialize<RecipeIngredient[]>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "recipe-ingredients.json"), cancellationToken), options)!;
            var recipeTags = JsonSerializer.Deserialize<RecipeTag[]>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "recipe-tags.json"), cancellationToken), options)!;
            var prep = JsonSerializer.Deserialize<Prep>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "prep.json"), cancellationToken), options)!;
            var prepIngredients = JsonSerializer.Deserialize<PrepIngredient[]>(
                await File.ReadAllTextAsync(Path.Combine(seedPath, "prep-ingredients.json"), cancellationToken), options)!;

            foreach (var tag in tags) tag.UserId = user.Id;

            await db.Users.AddAsync(user, cancellationToken);
            await db.Ingredients.AddRangeAsync(ingredients, cancellationToken);
            await db.RatingDimensions.AddRangeAsync(ratingDimensions, cancellationToken);
            await db.Tags.AddRangeAsync(tags, cancellationToken);
            await db.Recipes.AddAsync(recipe, cancellationToken);
            await db.RecipeIngredients.AddRangeAsync(recipeIngredients, cancellationToken);
            await db.RecipeTags.AddRangeAsync(recipeTags, cancellationToken);
            await db.Preps.AddAsync(prep, cancellationToken);
            await db.PrepIngredients.AddRangeAsync(prepIngredients, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var timestamp = DateTimeOffset.UtcNow;
        var systemUser = new Guid("00000000-0000-0000-0000-000000000001");
        var userId = userContext.InternalId ?? systemUser;

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
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PrepApi.Contracts;

namespace PrepApi.Data;

public class PrepDb(DbContextOptions<PrepDb> options, UserContext userContext) : DbContext(options)
{
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<Prep> Preps { get; set; }

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
                    bool addedData = false;

                    var flourId = new Guid("11111111-1111-1111-1111-111111111111");
                    var butterId = new Guid("22222222-2222-2222-2222-222222222222");
                    var sugarId = new Guid("33333333-3333-3333-3333-333333333333");
                    var saltId = new Guid("44444444-4444-4444-4444-444444444444");

                    var existingFlour = await prepDbContext.Set<Ingredient>().FindAsync([flourId], cancellationToken);
                    if (existingFlour == null)
                    {
                        prepDbContext.Set<Ingredient>().Add(new Ingredient
                        {
                            Id = flourId, Name = "Flour"
                        });
                        addedData = true;
                    }

                    var existingButter = await prepDbContext.Set<Ingredient>().FindAsync([butterId], cancellationToken);
                    if (existingButter == null)
                    {
                        prepDbContext.Set<Ingredient>().Add(new Ingredient
                        {
                            Id = butterId, Name = "Butter"
                        });
                        addedData = true;
                    }

                    var existingSugar = await prepDbContext.Set<Ingredient>().FindAsync([sugarId], cancellationToken);
                    if (existingSugar == null)
                    {
                        prepDbContext.Set<Ingredient>().Add(new Ingredient
                        {
                            Id = sugarId, Name = "Sugar"
                        });
                        addedData = true;
                    }

                    var existingSalt = await prepDbContext.Set<Ingredient>().FindAsync([saltId], cancellationToken);
                    if (existingSalt == null)
                    {
                        prepDbContext.Set<Ingredient>().Add(new Ingredient
                        {
                            Id = saltId, Name = "Salt"
                        });
                        addedData = true;
                    }

                    var recipeId = new Guid("55555555-5555-5555-5555-555555555555");
                    var existingRecipe = await prepDbContext.Set<Recipe>()
                        .Include(r => r.RecipeIngredients)
                        .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

                    if (existingRecipe == null)
                    {
                        var steps = new List<StepDto>
                        {
                            new() { Order = 1, Description = "Mix dry ingredients." },
                            new() { Order = 2, Description = "Add wet ingredients." },
                            new() { Order = 3, Description = "Cook until done." }
                        };

                        var newRecipe = new Recipe
                        {
                            Id = recipeId,
                            Name = "Seeded Recipe",
                            UserId = "SeedUser",
                            Description = "Basic recipe description.",
                            PrepTimeMinutes = 5,
                            CookTimeMinutes = 10,
                            Yield = "8 servings",
                            StepsJson = JsonSerializer.Serialize(steps),
                        };
                        prepDbContext.Set<Recipe>().Add(newRecipe);

                        prepDbContext.Set<RecipeIngredient>().AddRange(
                            new RecipeIngredient
                                { RecipeId = recipeId, IngredientId = flourId, Quantity = 2, Unit = Unit.Whole },
                            new RecipeIngredient
                                { RecipeId = recipeId, IngredientId = butterId, Quantity = 3, Unit = Unit.Gram },
                            new RecipeIngredient
                                { RecipeId = recipeId, IngredientId = sugarId, Quantity = 4, Unit = Unit.Kilogram }
                        );
                        addedData = true;
                    }

                    if (addedData)
                    {
                        await prepDbContext.SaveChangesAsync(cancellationToken);
                    }
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
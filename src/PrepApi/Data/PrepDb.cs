using Microsoft.EntityFrameworkCore;

namespace PrepApi.Data;

public class PrepDb(DbContextOptions<PrepDb> options, UserContext userContext) : DbContext(options)
{
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

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
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(256);

            // TODO Remove after migrations
            var utcNow = DateTimeOffset.UtcNow;
            entity.HasData(
                new Ingredient
                {
                    Id = new Guid("11111111-1111-1111-1111-111111111111"),
                    Name = "All-Purpose Flour",
                    CreatedAt = utcNow,
                    CreatedBy = "Seed",
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Ingredient
                {
                    Id = new Guid("22222222-2222-2222-2222-222222222222"),
                    Name = "Butter",
                    CreatedAt = utcNow,
                    CreatedBy = "Seed",
                }
            );
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
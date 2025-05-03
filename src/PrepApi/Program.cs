using Microsoft.EntityFrameworkCore;

using FluentValidation;

using PrepApi;
using PrepApi.Data;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<PrepDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<UserContext>();

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PrepDb>("Database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.MapScalarApiReference();

    // TODO remove after initial
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRecipeEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
using System.Security.Claims;

using Azure.Monitor.OpenTelemetry.AspNetCore;

using FluentValidation;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using PrepApi.Data;
using PrepApi.Extensions;
using PrepApi.Ingredients;
using PrepApi.Preps;
using PrepApi.Recipes;
using PrepApi.Shared.Services;
using PrepApi.Users;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppServices();

if (!builder.Environment.IsEnvironment(Environments.Development))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddDefaultCorsPolicy(builder.Configuration);

builder.Services.AddOpenApi(options => options.AddBearerTokenAuthentication());
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<PrepDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

builder.Services.AddUserContext();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks().AddDbContextCheck<PrepDb>("Database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PrepDb>();
    // await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("ApiDocsEnabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Servers = [];
        options.Authentication = new() { PreferredSecurityScheme = "Bearer" };
    });
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRecipeEndpoints();
app.MapTagEndpoints();
app.MapPrepEndpoints();
app.MapUserEndpoints();
app.MapIngredientEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
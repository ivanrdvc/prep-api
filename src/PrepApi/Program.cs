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
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("ApiDocsEnabled"))
{
    app.MapOpenApi().CacheOutput();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRecipeEndpoints();
app.MapPrepEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
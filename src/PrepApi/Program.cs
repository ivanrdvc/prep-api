using FluentValidation;

using Microsoft.EntityFrameworkCore;

using PrepApi;
using PrepApi.Data;
using PrepApi.Extensions;
using PrepApi.Preps;
using PrepApi.Recipes;
using PrepApi.Shared.Queue;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppServices();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<PrepDb>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PrepDb>("Database");

builder.Services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();
builder.Services.AddHostedService<TaskProcessor>();

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
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRecipeEndpoints();
app.MapPrepEndpoints();
app.MapTagEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
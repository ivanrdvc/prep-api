using System.ClientModel;

using Azure.AI.OpenAI;

using Microsoft.Extensions.AI;

using PrepApi.Preps;
using PrepApi.Shared.Queue;
using PrepApi.Shared.Services;

namespace PrepApi.Extensions;

public static class Extensions
{
    public static void AddAppServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAzureOpenAiServices(builder.Configuration);

        builder.Services.AddScoped<PrepService>();
        builder.Services.AddScoped<RecipeInsightService>();
        builder.Services.AddHttpClient<UsdaApiService>();

        builder.Services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();

        builder.Services.AddHostedService<SupabaseKeepAliveService>();
        builder.Services.AddHostedService<TaskProcessor>();
    }

    private static void AddAzureOpenAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["AI:AzureOpenAI:Endpoint"];
        var apiKey = configuration["AI:AzureOpenAI:Key"];
        var modelId = configuration["AI:AzureOpenAI:Chat:ModelId"];

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("AI:AzureOpenAI:Endpoint configuration is required");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("AI:AzureOpenAI:Key configuration is required");

        if (string.IsNullOrWhiteSpace(modelId))
            throw new InvalidOperationException("AI:AzureOpenAI:Chat:ModelId configuration is required");

        services.AddSingleton(new AzureOpenAIClient(
            new Uri(endpoint),
            new ApiKeyCredential(apiKey)
        ));

        services.AddChatClient(serviceProvider =>
        {
            var azureClient = serviceProvider.GetRequiredService<AzureOpenAIClient>();
            return azureClient.AsChatClient(modelId);
        });
    }

    public static IServiceCollection AddDefaultCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}
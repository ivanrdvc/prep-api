using Microsoft.EntityFrameworkCore;

using PrepApi.Data;

namespace PrepApi.Shared.Services;

public class SupabaseKeepAliveService(
    ILogger<SupabaseKeepAliveService> logger, 
    IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrepDb>();
            await db.Ingredients.FirstOrDefaultAsync(stoppingToken);
            logger.LogInformation("Daily task running at: {time}", DateTime.Now);
        }
    }
}
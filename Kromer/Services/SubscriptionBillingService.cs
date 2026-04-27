using Kromer.Repositories;

namespace Kromer.Services;

public class SubscriptionBillingService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionBillingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<SubscriptionRepository>();
                var processed = await repository.BillDueSubscriptionsAsync(stoppingToken);

                if (processed > 0)
                {
                    logger.LogInformation("Processed {Count} due subscription payments", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process due subscription payments");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

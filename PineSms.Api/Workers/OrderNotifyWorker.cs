using PineSms.Api.Queue;
using PineSms.Core.Contracts;

namespace PineSms.Api.Workers;

public sealed class OrderNotifyWorker(OrderNotifyQueue queue, IServiceScopeFactory scopeFactory, ILogger<OrderNotifyWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var result = await orderService.NotifyOrder(command);
                if (!result.Success)
                    logger.LogWarning("NotifyOrder failed for order {OrderCode}: {Message}", command.OrderCode, result.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing order notification for {OrderCode}", command.OrderCode);
            }
        }
    }
}

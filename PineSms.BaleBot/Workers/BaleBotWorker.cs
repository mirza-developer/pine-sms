using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PineSms.BaleBot.Services;

namespace PineSms.BaleBot.Workers;

/// <summary>
/// Background worker that continuously long-polls the Bale Bot API for new updates
/// and dispatches them to <see cref="IBotUpdateHandler"/>.
/// </summary>
public class BaleBotWorker : BackgroundService
{
    /// <summary>Timeout in seconds for each long-poll request sent to getUpdates.</summary>
    private const int LongPollTimeoutSeconds = 30;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly BaleBotClient botClient;
    private readonly ILogger<BaleBotWorker> logger;

    public BaleBotWorker(IServiceScopeFactory scopeFactory, BaleBotClient botClient, ILogger<BaleBotWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.botClient = botClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BaleBotWorker started");

        long offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await botClient.GetUpdatesAsync(offset, LongPollTimeoutSeconds, stoppingToken);

                foreach (var update in updates)
                {
                    // Advance the offset so processed updates are acknowledged on the next poll
                    if (update.UpdateId >= offset)
                        offset = update.UpdateId + 1;

                    // Each update is handled in its own DI scope so scoped services (e.g. DbContext) are fresh
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IBotUpdateHandler>();
                    try
                    {
                        await handler.HandleAsync(update, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error handling update {UpdateId}", update.UpdateId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in BaleBotWorker poll loop; retrying in 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("BaleBotWorker stopped");
    }
}

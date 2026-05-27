using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PineSms.BaleBot.Models;
using PineSms.BaleBot.Services;

namespace PineSms.BaleBot.Workers;

/// <summary>
/// Background worker that continuously long-polls the Bale Bot API for new updates
/// and dispatches them to <see cref="IBotUpdateHandler"/>.
/// Updates are processed concurrently with a semaphore limit to prevent resource exhaustion,
/// while maintaining per-user message order to prevent mixed responses.
/// </summary>
public class BaleBotWorker : BackgroundService
{
    /// <summary>Timeout in seconds for each long-poll request sent to getUpdates.</summary>
    private const int LongPollTimeoutSeconds = 30;

    /// <summary>Maximum number of concurrent update handlers to prevent server resource exhaustion.</summary>
    private const int MaxConcurrentUpdates = 10;

    /// <summary>Timeout for AI and database operations per user request.</summary>
    private const int UpdateTimeoutSeconds = 60;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly BaleBotClient botClient;
    private readonly ILogger<BaleBotWorker> logger;
    private readonly SemaphoreSlim concurrencySemaphore = new(MaxConcurrentUpdates, MaxConcurrentUpdates);
    private readonly ConcurrentDictionary<long, SemaphoreSlim> perUserSemaphores = new();

    public BaleBotWorker(IServiceScopeFactory scopeFactory, BaleBotClient botClient, ILogger<BaleBotWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.botClient = botClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BaleBotWorker started with concurrent processing (max {MaxConcurrent} concurrent updates)", MaxConcurrentUpdates);

        long offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await botClient.GetUpdatesAsync(offset, LongPollTimeoutSeconds, stoppingToken);

                // Process updates concurrently while maintaining per-user order
                var tasks = new List<Task>();

                foreach (var update in updates)
                {
                    if (update.UpdateId >= offset)
                        offset = update.UpdateId + 1;

                    // Fire and forget - don't await here to allow concurrent processing
                    tasks.Add(ProcessUpdateAsync(update, stoppingToken));
                }

                // Wait for all updates in this batch to complete before polling again
                // This prevents offset from advancing too quickly while updates are still processing
                await Task.WhenAll(tasks);
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

    /// <summary>
    /// Processes a single update with concurrency control and per-user ordering.
    /// Uses a global semaphore to limit total concurrent processing and per-user
    /// semaphores to ensure messages from the same user are processed in order.
    /// </summary>
    private async Task ProcessUpdateAsync(BaleUpdate update, CancellationToken stoppingToken)
    {
        // Extract chat ID for per-user serialization (0 if message is null)
        long chatId = update.Message?.Chat?.Id ?? 0;

        // Get or create a per-user semaphore to maintain message order for this user
        var userSemaphore = perUserSemaphores.GetOrAdd(chatId, _ => new SemaphoreSlim(1, 1));

        // Wait for global concurrency limit
        await concurrencySemaphore.WaitAsync(stoppingToken);

        try
        {
            // Wait for this user's previous messages to complete (per-user ordering)
            await userSemaphore.WaitAsync(stoppingToken);

            try
            {
                // Create a timeout for this specific update processing
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(UpdateTimeoutSeconds));

                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IBotUpdateHandler>();

                await handler.HandleAsync(update, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Update {UpdateId} processing timed out after {Timeout}s",
                    update.UpdateId, UpdateTimeoutSeconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling update {UpdateId}", update.UpdateId);
            }
            finally
            {
                userSemaphore.Release();
            }
        }
        finally
        {
            concurrencySemaphore.Release();
        }
    }

    public override void Dispose()
    {
        concurrencySemaphore.Dispose();
        foreach (var semaphore in perUserSemaphores.Values)
        {
            semaphore.Dispose();
        }
        base.Dispose();
    }
}

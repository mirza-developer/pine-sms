using PineAI.BaleBot.Services;

namespace PineAI.BaleBot.Workers;

/// <summary>
/// Periodically evicts expired penalty entries from <see cref="UserPenaltyStore"/>.
/// Runs on the same interval as <see cref="UserPenaltyStore.PenaltyDuration"/> (10 minutes)
/// so stale entries are removed promptly.
/// </summary>
public class PenaltyStoreCleanupWorker(
    UserPenaltyStore penaltyStore,
    ILogger<PenaltyStoreCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(UserPenaltyStore.PenaltyDuration, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var evicted = penaltyStore.EvictExpired();
            if (evicted > 0)
                logger.LogInformation("UserPenaltyStore: evicted {Count} expired penalty entr{Suffix}",
                    evicted, evicted == 1 ? "y" : "ies");
        }
    }
}

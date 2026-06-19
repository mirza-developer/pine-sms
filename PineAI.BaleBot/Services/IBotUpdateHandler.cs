using PineAI.BaleBot.Models;

namespace PineAI.BaleBot.Services;

/// <summary>Processes a single incoming Bale update.</summary>
public interface IBotUpdateHandler
{
    Task HandleAsync(BaleUpdate update, CancellationToken ct);
}

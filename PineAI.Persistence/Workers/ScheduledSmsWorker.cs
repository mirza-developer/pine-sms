using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PineAI.Core.Entities;
using PineAI.Persistence.Repositories;
using PineAI.Persistence.Services;

namespace PineAI.Persistence.Workers;

/// <summary>
/// Background service that polls the database every minute and sends any
/// SmsSendJobParts whose ScheduledAt time has arrived.
/// </summary>
public class ScheduledSmsWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ScheduledSmsWorker> logger;

    public ScheduledSmsWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledSmsWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScheduledSmsWorker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueParts(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error while processing scheduled SMS parts.");
            }
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDueParts(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PineAIDbContext>();
        var smsRepo = scope.ServiceProvider.GetRequiredService<SmsRepository>();

        var now = DateTime.Now;

        // Load pending parts that are due, together with the parent job
        var dueParts = await db.SmsSendJobPart
            .Include(p => p.Job)
            .Where(p => p.Status == SmsJobPartStatus.Pending && p.ScheduledAt <= now)
            .OrderBy(p => p.ScheduledAt)
            .ToListAsync(ct);

        if (dueParts.Count == 0) return;

        logger.LogInformation("Processing {Count} due SMS job parts.", dueParts.Count);

        // Process each part individually with immediate status persistence
        foreach (var part in dueParts)
        {
            try
            {
                // Mark as sending and save immediately
                part.Status = SmsJobPartStatus.Sending;
                await db.SaveChangesAsync(ct);

                var customerIds = JsonSerializer.Deserialize<List<int>>(part.CustomerIdsJson) ?? new();
                var customers = await db.Customer
                    .Where(c => customerIds.Contains(c.Id))
                    .ToListAsync(ct);

                var phoneNumbers = customers.Select(c => "0" + c.PhoneNumber).ToList();

                var results = await smsRepo.SendToMelipayamak(
                    part.Job.FromNumber, phoneNumbers, part.Job.MessageText);

                var executedAt = DateTime.Now;
                foreach (var customer in customers)
                    customer.LastUsageDate = executedAt;

                part.ExecutedAt = executedAt;

                if (results.Submitted)
                {
                    part.Status = SmsJobPartStatus.Completed;
                    part.SentCount = phoneNumbers.Count;
                    part.ResultJson = JsonSerializer.Serialize(results.Recipients);

                    var smsLog = new SmsLog
                    {
                        SendDate = executedAt,
                        SendUserId = part.Job.UserId,
                        MessageText = part.Job.MessageText,
                        FromNumber = part.Job.FromNumber,
                        RecipientsJson = JsonSerializer.Serialize(results.Recipients)
                    };
                    db.SmsLog.Add(smsLog);

                    logger.LogInformation(
                        "Job {JobId} part {PartNumber}: submitted to provider for {Count} recipients.",
                        part.JobId, part.PartNumber, phoneNumbers.Count);
                }
                else
                {
                    part.Status = SmsJobPartStatus.Failed;
                    part.ResultJson = JsonSerializer.Serialize(new { error = results.ErrorMessage });
                    logger.LogWarning(
                        "Job {JobId} part {PartNumber}: provider rejected submission. Error: {Error}",
                        part.JobId, part.PartNumber, results.ErrorMessage);
                }

                // Save the final status
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Ensure failure status is persisted even on exception
                try
                {
                    part.Status = SmsJobPartStatus.Failed;
                    part.ExecutedAt = DateTime.Now;
                    part.ResultJson = JsonSerializer.Serialize(new { error = ex.Message });
                    await db.SaveChangesAsync(ct);
                    logger.LogError(ex, "Failed to send job {JobId} part {PartNumber}.", part.JobId, part.PartNumber);
                }
                catch (Exception saveEx)
                {
                    logger.LogError(saveEx, "Failed to save error status for job {JobId} part {PartNumber}.", part.JobId, part.PartNumber);
                }
            }
        }
    }
}

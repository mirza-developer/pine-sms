using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PineSms.Core.Entities;
using PineSms.Persistence.Repositories;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Workers;

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
        var db = scope.ServiceProvider.GetRequiredService<PineSmsDbContext>();
        var smsRepo = scope.ServiceProvider.GetRequiredService<SmsRepository>();

        var now = DateTime.UtcNow;

        // Load pending parts that are due, together with the parent job
        var dueParts = await db.SmsSendJobPart
            .Include(p => p.Job)
            .Where(p => p.Status == SmsJobPartStatus.Pending && p.ScheduledAt <= now)
            .OrderBy(p => p.ScheduledAt)
            .ToListAsync(ct);

        if (dueParts.Count == 0) return;

        logger.LogInformation("Processing {Count} due SMS job parts.", dueParts.Count);

        foreach (var part in dueParts)
        {
            part.Status = SmsJobPartStatus.Sending;
        }
        await db.SaveChangesAsync(ct);

        foreach (var part in dueParts)
        {
            try
            {
                var customerIds = JsonSerializer.Deserialize<List<int>>(part.CustomerIdsJson) ?? new();
                var customers = await db.Customer
                    .Where(c => customerIds.Contains(c.Id))
                    .ToListAsync(ct);

                var phoneNumbers = customers.Select(c => "0" + c.PhoneNumber).ToList();

                var results = await smsRepo.SendToMelipayamak(
                    part.Job.FromNumber, phoneNumbers, part.Job.MessageText);

                var executedAt = DateTime.UtcNow;
                foreach (var customer in customers)
                    customer.LastUsageDate = executedAt;

                part.Status = SmsJobPartStatus.Completed;
                part.SentCount = phoneNumbers.Count;
                part.ExecutedAt = executedAt;
                part.ResultJson = JsonSerializer.Serialize(results);

                var smsLog = new SmsLog
                {
                    SendDate = executedAt,
                    SendUserId = part.Job.UserId,
                    MessageText = part.Job.MessageText,
                    FromNumber = part.Job.FromNumber,
                    RecipientsJson = JsonSerializer.Serialize(results)
                };
                db.SmsLog.Add(smsLog);

                logger.LogInformation(
                    "Job {JobId} part {PartNumber}: sent to {Count} recipients.",
                    part.JobId, part.PartNumber, phoneNumbers.Count);
            }
            catch (Exception ex)
            {
                part.Status = SmsJobPartStatus.Failed;
                part.ExecutedAt = DateTime.UtcNow;
                part.ResultJson = JsonSerializer.Serialize(new { error = ex.Message });
                logger.LogError(ex, "Failed to send job {JobId} part {PartNumber}.", part.JobId, part.PartNumber);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}

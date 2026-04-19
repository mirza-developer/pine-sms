using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PineSms.Core.Contracts;
using PineSms.Core.Entities;
using PineSms.Core.Features.Sms;
using PineSms.Persistence.Services;

namespace PineSms.Persistence.Repositories;

public class SmsRepository : ISmsService
{
    private readonly PineSmsDbContext dbContext;
    private readonly IHttpClientFactory httpClientFactory;

    public SmsRepository(PineSmsDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        this.dbContext = dbContext;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<SendSmsResult> SendSms(SendSmsCommand command, string userId)
    {
        var customers = await dbContext.Customer
            .Where(c => command.CustomerIds.Contains(c.Id))
            .ToListAsync();

        if (customers.Count == 0)
            return new SendSmsResult { Success = false, Message = "مشتری انتخاب نشده است" };

        var phoneNumbers = customers.Select(c => "0" + c.PhoneNumber).ToList();

        var recipientResults = await SendToMelipayamak(command.FromNumber, phoneNumbers, command.MessageText);

        var now = DateTime.UtcNow;
        foreach (var customer in customers)
            customer.LastUsageDate = now;

        var smsLog = new SmsLog
        {
            SendDate = now,
            SendUserId = userId,
            MessageText = command.MessageText,
            FromNumber = command.FromNumber,
            RecipientsJson = JsonSerializer.Serialize(recipientResults)
        };

        dbContext.SmsLog.Add(smsLog);
        await dbContext.SaveChangesAsync();

        return new SendSmsResult
        {
            Success = true,
            SentCount = phoneNumbers.Count,
            Message = $"پیامک برای {phoneNumbers.Count} نفر ارسال شد"
        };
    }

    public async Task<ScheduleSmsResult> ScheduleSms(ScheduleSmsCommand command, string userId)
    {
        if (command.CustomerIds.Count == 0)
            return new ScheduleSmsResult { Success = false, Message = "مشتری انتخاب نشده است" };

        int parts = Math.Max(1, command.NumberOfParts);
        var allIds = command.CustomerIds.ToList();
        int chunkSize = (int)Math.Ceiling(allIds.Count / (double)parts);

        var job = new SmsSendJob
        {
            UserId = userId,
            FromNumber = command.FromNumber,
            MessageText = command.MessageText,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.SmsSendJob.Add(job);
        await dbContext.SaveChangesAsync();

        var firstSendAt = command.FirstSendAt ?? DateTime.UtcNow;

        for (int i = 0; i < parts; i++)
        {
            var chunk = allIds.Skip(i * chunkSize).Take(chunkSize).ToList();
            if (chunk.Count == 0) break;

            var part = new SmsSendJobPart
            {
                JobId = job.Id,
                PartNumber = i + 1,
                ScheduledAt = firstSendAt.AddMinutes(i * command.DelayMinutesBetweenParts),
                CustomerIdsJson = JsonSerializer.Serialize(chunk),
                Status = SmsJobPartStatus.Pending
            };
            dbContext.SmsSendJobPart.Add(part);
        }

        await dbContext.SaveChangesAsync();

        return new ScheduleSmsResult
        {
            Success = true,
            JobId = job.Id,
            Message = $"ارسال زمان‌بندی شد ({parts} قسمت)"
        };
    }

    public async Task<List<SmsSendJobDto>> GetSmsJobs(string userId)
    {
        var jobs = await dbContext.SmsSendJob
            .Where(j => j.UserId == userId)
            .Include(j => j.Parts)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return jobs.Select(MapJobToDto).ToList();
    }

    public async Task<SmsSendJobDto?> GetSmsJob(int jobId, string userId)
    {
        var job = await dbContext.SmsSendJob
            .Include(j => j.Parts)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);

        return job == null ? null : MapJobToDto(job);
    }

    private static SmsSendJobDto MapJobToDto(SmsSendJob job) => new()
    {
        Id = job.Id,
        FromNumber = job.FromNumber,
        MessageText = job.MessageText,
        CreatedAt = job.CreatedAt,
        Parts = job.Parts
            .OrderBy(p => p.PartNumber)
            .Select(p => new SmsSendJobPartDto
            {
                Id = p.Id,
                PartNumber = p.PartNumber,
                ScheduledAt = p.ScheduledAt,
                RecipientCount = JsonSerializer.Deserialize<List<int>>(p.CustomerIdsJson)?.Count ?? 0,
                Status = p.Status.ToString(),
                SentCount = p.SentCount,
                ExecutedAt = p.ExecutedAt,
                ResultJson = p.ResultJson
            })
            .ToList()
    };

    /// <summary>Sends SMS via Melipayamak and returns per-phone result objects.</summary>
    internal async Task<List<object>> SendToMelipayamak(string fromNumber, List<string> phoneNumbers, string messageText)
    {
        var recipientResults = new List<object>();
        try
        {
            using var client = httpClientFactory.CreateClient("Melipayamak");
            var payload = new
            {
                from = fromNumber,
                to = phoneNumbers.ToArray(),
                text = messageText,
                udh = ""
            };

            var response = await client.PostAsJsonAsync(
                "api/send/advanced/f46fefd347444ded90bda092cde7f6f2", payload);

            var apiResponse = await response.Content.ReadAsStringAsync();

            foreach (var phone in phoneNumbers)
                recipientResults.Add(new { phone, result = apiResponse, success = response.IsSuccessStatusCode });
        }
        catch (Exception ex)
        {
            foreach (var phone in phoneNumbers)
                recipientResults.Add(new { phone, result = ex.Message, success = false });
        }
        return recipientResults;
    }
}

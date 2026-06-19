using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PineAI.Core.Contracts;
using PineAI.Core.Entities;
using PineAI.Core.Features.Sms;
using PineAI.Persistence.Services;

namespace PineAI.Persistence.Repositories;

public class SmsRepository : ISmsService
{
    private readonly PineAIDbContext dbContext;
    private readonly IHttpClientFactory httpClientFactory;

    public SmsRepository(PineAIDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        this.dbContext = dbContext;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<SendSmsResult> SendSms(SendSmsCommand command, string userId)
    {
        // Always include tester customers, deduped with the selected ones
        var testerIds = await dbContext.Customer
            .Where(c => c.IsTester)
            .Select(c => c.Id)
            .ToListAsync();

        var allIds = command.CustomerIds.Union(testerIds).ToList();

        var customers = await dbContext.Customer
            .Where(c => allIds.Contains(c.Id))
            .ToListAsync();

        if (customers.Count == 0)
            return new SendSmsResult { Success = false, Message = "مشتری انتخاب نشده است" };

        var phoneNumbers = customers.Select(c => "0" + c.PhoneNumber).ToList();

        var sendResult = await SendToMelipayamak(command.FromNumber, phoneNumbers, command.MessageText);

        if (!sendResult.Submitted)
            return new SendSmsResult { Success = false, Message = sendResult.ErrorMessage ?? "خطا در ارسال پیامک" };

        var now = DateTime.Now;
        foreach (var customer in customers)
            customer.LastUsageDate = now;

        var smsLog = new SmsLog
        {
            SendDate = now,
            SendUserId = userId,
            MessageText = command.MessageText,
            FromNumber = command.FromNumber,
            RecipientsJson = JsonSerializer.Serialize(sendResult.Recipients)
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

        // Fetch tester customer IDs – they will be appended to every chunk
        var testerIds = await dbContext.Customer
            .Where(c => c.IsTester)
            .Select(c => c.Id)
            .ToListAsync();

        int parts = Math.Max(1, command.NumberOfParts);
        var allIds = command.CustomerIds.ToList();
        int chunkSize = (int)Math.Ceiling(allIds.Count / (double)parts);

        var job = new SmsSendJob
        {
            UserId = userId,
            FromNumber = command.FromNumber,
            MessageText = command.MessageText,
            CreatedAt = DateTime.Now
        };
        dbContext.SmsSendJob.Add(job);
        await dbContext.SaveChangesAsync();

        var firstSendAt = command.FirstSendAt ?? DateTime.Now;

        for (int i = 0; i < parts; i++)
        {
            var chunk = allIds.Skip(i * chunkSize).Take(chunkSize).ToList();
            if (chunk.Count == 0) break;

            // Merge tester IDs into every chunk (deduped)
            var chunkWithTesters = chunk.Union(testerIds).ToList();

            var part = new SmsSendJobPart
            {
                JobId = job.Id,
                PartNumber = i + 1,
                ScheduledAt = firstSendAt.AddMinutes(i * command.DelayMinutesBetweenParts),
                CustomerIdsJson = JsonSerializer.Serialize(chunkWithTesters),
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

    public async Task<GetDeliveryStatusResult> GetSmsDeliveryStatus(long[] recIds)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("Melipayamak");
            var response = await client.PostAsJsonAsync(
                "api/receive/status/f46fefd347444ded90bda092cde7f6f2",
                new { recIds });

            var apiJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new GetDeliveryStatusResult
                {
                    Success = false,
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {apiJson}"
                };

            var apiResponse = JsonSerializer.Deserialize<MelipayamakStatusApiResponse>(apiJson);
            var results = apiResponse?.Results ?? Array.Empty<string>();
            var codes = apiResponse?.ResultsAsCode ?? Array.Empty<int>();

            var statuses = recIds.Select((id, idx) => new RecIdStatus
            {
                RecId = id,
                Result = idx < results.Length ? results[idx] : string.Empty,
                ResultCode = idx < codes.Length ? codes[idx] : 0
            }).ToList();

            return new GetDeliveryStatusResult
            {
                Success = true,
                ProviderStatus = apiResponse?.Status ?? string.Empty,
                Statuses = statuses
            };
        }
        catch (Exception ex)
        {
            return new GetDeliveryStatusResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
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

    /// <summary>
    /// Sends a batch SMS via Melipayamak.
    /// Returns <see cref="MelipayamakSendResult.Submitted"/> = <c>true</c> when the provider
    /// accepted the request (HTTP 2xx); each recipient then has a <c>RecId</c> for later
    /// delivery-status queries. Any HTTP error or exception sets <c>Submitted = false</c>.
    /// </summary>
    internal async Task<MelipayamakSendResult> SendToMelipayamak(string fromNumber, List<string> phoneNumbers, string messageText)
    {
        List<string> errorStrings = new()
        {
            "محدودیت در حجم ارسال"
        };

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

            var apiJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode
                || errorStrings.Any(a=> apiJson.Contains(a)))
                return new MelipayamakSendResult
                {
                    Submitted = false,
                    Recipients = phoneNumbers.Select(p => new SmsRecipientRecord { Phone = p }).ToList(),
                    ErrorMessage = $"HTTP {(int)response.StatusCode}: {apiJson}"
                };

            var apiResponse = JsonSerializer.Deserialize<MelipayamakSendApiResponse>(apiJson);
            var recIds = apiResponse?.RecIds ?? Array.Empty<long>();

            var recipients = phoneNumbers.Select((phone, idx) => new SmsRecipientRecord
            {
                Phone = phone,
                RecId = idx < recIds.Length ? recIds[idx] : null
            }).ToList();

            return new MelipayamakSendResult
            {
                Submitted = true,
                Recipients = recipients,
                ProviderStatus = apiResponse?.Status ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            return new MelipayamakSendResult
            {
                Submitted = false,
                Recipients = phoneNumbers.Select(p => new SmsRecipientRecord { Phone = p }).ToList(),
                ErrorMessage = ex.Message
            };
        }

       
    }
}

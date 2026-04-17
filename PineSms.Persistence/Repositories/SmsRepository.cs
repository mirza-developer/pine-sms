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

        var recipientResults = new List<object>();

        try
        {
            using var client = httpClientFactory.CreateClient("Melipayamak");
            var payload = new
            {
                from = command.FromNumber,
                to = phoneNumbers.ToArray(),
                text = command.MessageText,
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
}

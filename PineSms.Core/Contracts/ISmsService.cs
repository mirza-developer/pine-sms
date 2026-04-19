using PineSms.Core.Entities;
using PineSms.Core.Features.Sms;

namespace PineSms.Core.Contracts;

public interface ISmsService
{
    Task<SendSmsResult> SendSms(SendSmsCommand command, string userId);
    Task<ScheduleSmsResult> ScheduleSms(ScheduleSmsCommand command, string userId);
    Task<List<SmsSendJobDto>> GetSmsJobs(string userId);
    Task<SmsSendJobDto?> GetSmsJob(int jobId, string userId);
}

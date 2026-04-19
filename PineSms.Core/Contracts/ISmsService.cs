using PineSms.Core.Features.Sms;

namespace PineSms.Core.Contracts;

public interface ISmsService
{
    Task<SendSmsResult> SendSms(SendSmsCommand command, string userId);
}

namespace PineAI.Core.Contracts;

public interface IBaleMessengerService
{
    /// <summary>Sends a text message to the given phone number via Bale Safir API.</summary>
    Task<bool> SendMessageAsync(string phoneNumber, string message);
}

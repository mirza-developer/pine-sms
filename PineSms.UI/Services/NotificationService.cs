using PineSms.Core.Dtos;

namespace PineSms.UI.Services;

public class NotificationService
{
    private readonly List<NotificationMessage> messages = new();
    public event Action? OnChange;

    public IReadOnlyList<NotificationMessage> Messages => messages.AsReadOnly();

    public void ShowSuccess(string message, NotificationPosition position = NotificationPosition.TopRight)
        => Add(message, NotificationLevel.Success, position);

    public void ShowError(string message, NotificationPosition position = NotificationPosition.TopRight)
        => Add(message, NotificationLevel.Error, position);

    public void ShowWarning(string message, NotificationPosition position = NotificationPosition.TopRight)
        => Add(message, NotificationLevel.Warning, position);

    public void ShowInformation(string message, NotificationPosition position = NotificationPosition.TopRight)
        => Add(message, NotificationLevel.Information, position);

    private async void Add(string message, NotificationLevel level, NotificationPosition position)
    {
        var notification = new NotificationMessage
        {
            Message = message,
            Level = level,
            Position = position
        };
        messages.Add(notification);
        OnChange?.Invoke();

        await Task.Delay(5000);

        messages.Remove(notification);
        OnChange?.Invoke();
    }

    public void Remove(Guid id)
    {
        var msg = messages.FirstOrDefault(m => m.Id == id);
        if (msg != null)
        {
            messages.Remove(msg);
            OnChange?.Invoke();
        }
    }
}

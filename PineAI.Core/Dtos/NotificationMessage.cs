namespace PineAI.Core.Dtos;

public class NotificationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public NotificationLevel Level { get; set; }
    public NotificationPosition Position { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsVisible { get; set; } = true;
}

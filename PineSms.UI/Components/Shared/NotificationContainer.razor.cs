using Microsoft.AspNetCore.Components;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Shared;

public partial class NotificationContainer : IDisposable
{
    [Parameter] public NotificationPosition Position { get; set; } = NotificationPosition.TopRight;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private IEnumerable<NotificationMessage> Notifications =>
        NotificationService.Messages.Where(m => m.Position == Position && m.IsVisible);

    protected override void OnInitialized()
    {
        NotificationService.OnChange += StateHasChanged;
    }

    private void Close(Guid id) => NotificationService.Remove(id);

    private static string GetIcon(NotificationLevel level) => level switch
    {
        NotificationLevel.Success => "✓",
        NotificationLevel.Error => "✕",
        NotificationLevel.Warning => "⚠",
        _ => "ℹ"
    };

    public void Dispose()
    {
        NotificationService.OnChange -= StateHasChanged;
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PineSms.Core.Features.BotConversation;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class BotConversations
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private string searchUsername = string.Empty;
    private string lastSearchedUsername = string.Empty;
    private List<BotChatMessageDto> messages = new();
    private bool isLoading = false;
    private bool hasSearched = false;

    private async Task LoadConversation()
    {
        if (string.IsNullOrWhiteSpace(searchUsername))
        {
            NotificationService.ShowError("لطفاً نام کاربری بله را وارد کنید");
            return;
        }

        isLoading = true;
        hasSearched = false;
        messages = new();

        try
        {
            lastSearchedUsername = searchUsername.Trim();
            var result = await ApiClient.GetBotConversationAsync(lastSearchedUsername);
            messages = result ?? new();
            hasSearched = true;
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری مکالمات");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await LoadConversation();
    }
}

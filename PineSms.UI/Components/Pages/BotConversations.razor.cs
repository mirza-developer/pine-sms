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

    private const int PageSize = 10;

    // ── Grid state ──────────────────────────────────────────────────────────
    private bool isGridLoading = false;
    private BotUserSummaryPageResult? currentPage;

    /// <summary>Client-side page cache — avoids re-fetching pages already loaded.</summary>
    private readonly Dictionary<int, BotUserSummaryPageResult> pageCache = new();

    // ── Chat state ──────────────────────────────────────────────────────────
    private string? selectedUsername;
    private List<BotChatMessageDto> messages = new();
    private bool isChatLoading = false;

    // ── Search ──────────────────────────────────────────────────────────────
    private string searchUsername = string.Empty;

    // ────────────────────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        await LoadGridPage(1);
    }

    // ── Grid navigation ─────────────────────────────────────────────────────

    private async Task GoToPage(int page)
    {
        if (currentPage is not null && page == currentPage.Page) return;
        await LoadGridPage(page);
    }

    private async Task PrevPage()
    {
        if (currentPage is not null && currentPage.Page > 1)
            await LoadGridPage(currentPage.Page - 1);
    }

    private async Task NextPage()
    {
        if (currentPage is not null && currentPage.Page < currentPage.TotalPages)
            await LoadGridPage(currentPage.Page + 1);
    }

    private async Task LoadGridPage(int page)
    {
        // Serve from cache if available
        if (pageCache.TryGetValue(page, out var cached))
        {
            currentPage = cached;
            return;
        }

        isGridLoading = true;
        try
        {
            var result = await ApiClient.GetBotUserSummariesAsync(page, PageSize);
            if (result is not null)
            {
                pageCache[page] = result;
                currentPage = result;
            }
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری لیست کاربران");
        }
        finally
        {
            isGridLoading = false;
        }
    }

    // ── Chat view ────────────────────────────────────────────────────────────

    private async Task OpenChat(string username)
    {
        selectedUsername = username;
        messages = new();
        isChatLoading = true;
        searchUsername = string.Empty;

        try
        {
            var result = await ApiClient.GetBotConversationAsync(username);
            messages = result ?? new();
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری مکالمات");
        }
        finally
        {
            isChatLoading = false;
        }
    }

    private void BackToGrid()
    {
        selectedUsername = null;
        messages = new();
        searchUsername = string.Empty;
    }

    // ── Search ───────────────────────────────────────────────────────────────

    private async Task HandleSearch()
    {
        if (string.IsNullOrWhiteSpace(searchUsername))
        {
            NotificationService.ShowError("لطفاً نام کاربری بله را وارد کنید");
            return;
        }
        await OpenChat(searchUsername.Trim());
    }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await HandleSearch();
    }
}

using Microsoft.AspNetCore.Components;
using PineAI.Core.Features.Account;
using PineAI.Core.Features.MenuLink;
using PineAI.UI.Services;

namespace PineAI.UI.Components.Pages;

public partial class MenuAccess
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool isLoading = true;
    private bool isLoadingLinks = false;
    private bool isSaving = false;
    private string saveMessage = string.Empty;

    private List<UserDto> users = [];
    private List<MenuLinkDto> allLinks = [];
    private UserDto? selectedUser;
    private HashSet<int> selectedLinkIds = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        if (!AuthState.IsAuthenticated || !AuthState.IsAdmin)
        {
            Navigation.NavigateTo("/access-denied", replace: true);
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var usersTask = ApiClient.GetAllUsersAsync();
            var linksTask = ApiClient.GetAllMenuLinksAsync();
            await Task.WhenAll(usersTask, linksTask);

            users = usersTask.Result ?? [];
            allLinks = linksTask.Result ?? [];
        }
        catch { }

        isLoading = false;
        StateHasChanged();
    }

    private async Task SelectUser(UserDto user)
    {
        selectedUser = user;
        selectedLinkIds = [];
        saveMessage = string.Empty;
        isLoadingLinks = true;
        StateHasChanged();

        try
        {
            var ids = await ApiClient.GetUserMenuLinkIdsAsync(user.Id);
            selectedLinkIds = [.. (ids ?? [])];
        }
        catch { }

        isLoadingLinks = false;
        StateHasChanged();
    }

    private void ToggleLink(int linkId, bool isChecked)
    {
        if (isChecked)
            selectedLinkIds.Add(linkId);
        else
            selectedLinkIds.Remove(linkId);
    }

    private async Task SaveLinks()
    {
        if (selectedUser is null) return;
        isSaving = true;
        saveMessage = string.Empty;
        StateHasChanged();

        bool ok = await ApiClient.SaveUserMenuLinksAsync(selectedUser.Id, [.. selectedLinkIds]);
        saveMessage = ok ? "ذخیره شد ✓" : "خطا در ذخیره‌سازی";

        isSaving = false;
        StateHasChanged();

        if (ok)
        {
            await Task.Delay(2000);
            saveMessage = string.Empty;
            StateHasChanged();
        }
    }
}

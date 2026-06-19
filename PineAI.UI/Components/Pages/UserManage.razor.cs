using Microsoft.AspNetCore.Components;
using PineAI.Core.Features.Account;
using PineAI.Core.Features.MenuLink;
using PineAI.UI.Services;

namespace PineAI.UI.Components.Pages;

public partial class UserManage
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private bool isLoading = true;
    private bool isLoadingLinks = false;
    private bool isSaving = false;
    private string saveMessage = string.Empty;
    private bool saveMessageSuccess = true;

    private List<UserDto> users = [];
    private List<MenuLinkDto> allLinks = [];
    private UserDto? selectedUser;
    private HashSet<int> selectedLinkIds = [];

    // Modal state
    private bool showModal = false;
    private bool isEditMode = false;
    private string modalError = string.Empty;
    private CreateUserCommand createCommand = new();
    private UpdateUserCommand updateCommand = new();
    private UserDto? deleteTarget;

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
            var usersTask = ApiClient.GetNonAdminUsersAsync();
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
        saveMessageSuccess = ok;
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

    // ── Add Modal ──────────────────────────────────────────────────────────
    private void OpenAddModal()
    {
        createCommand = new CreateUserCommand();
        modalError = string.Empty;
        isEditMode = false;
        showModal = true;
    }

    private async Task HandleCreate()
    {
        isSaving = true;
        modalError = string.Empty;
        StateHasChanged();

        var result = await ApiClient.CreateUserAsync(createCommand);
        if (result.Success)
        {
            showModal = false;
            await LoadAsync();
        }
        else
        {
            modalError = result.Message;
        }

        isSaving = false;
        StateHasChanged();
    }

    // ── Edit Modal ─────────────────────────────────────────────────────────
    private void OpenEditModal(UserDto user)
    {
        updateCommand = new UpdateUserCommand
        {
            Id = user.Id,
            PersianName = user.PersianName,
            NewPassword = null
        };
        modalError = string.Empty;
        isEditMode = true;
        showModal = true;
    }

    private async Task HandleUpdate()
    {
        isSaving = true;
        modalError = string.Empty;
        StateHasChanged();

        var result = await ApiClient.UpdateUserAsync(updateCommand.Id, updateCommand);
        if (result.Success)
        {
            showModal = false;
            await LoadAsync();
        }
        else
        {
            modalError = result.Message;
        }

        isSaving = false;
        StateHasChanged();
    }

    // ── Delete Modal ───────────────────────────────────────────────────────
    private void ConfirmDelete(UserDto user)
    {
        deleteTarget = user;
    }

    private async Task ExecuteDelete()
    {
        if (deleteTarget is null) return;
        isSaving = true;
        StateHasChanged();

        var targetId = deleteTarget.Id;
        var result = await ApiClient.DeleteUserAsync(targetId);
        deleteTarget = null;

        if (result.Success)
        {
            if (selectedUser?.Id == targetId)
                selectedUser = null;
            await LoadAsync();
        }

        isSaving = false;
        StateHasChanged();
    }

    private void CloseModal()
    {
        showModal = false;
        modalError = string.Empty;
    }
}

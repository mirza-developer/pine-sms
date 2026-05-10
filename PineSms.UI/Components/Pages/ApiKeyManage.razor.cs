using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PineSms.Core.Entities;
using PineSms.Core.Features.ApiKey;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class ApiKeyManage
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<ApiKey> keys = new();
    private bool isLoading = false;
    private bool isSaving = false;
    private bool showModal = false;
    private CreateApiKeyCommand createCommand = new() { ExpireAt = DateTime.Now.AddYears(1) };
    private string? expireAtPersian;
    private ApiKey? deleteTarget = null;
    private string newlyCreatedKey = string.Empty;

    protected override async Task OnInitializedAsync() => await LoadKeys();

    private async Task LoadKeys()
    {
        isLoading = true;
        try
        {
            var result = await ApiClient.GetApiKeysAsync();
            keys = result ?? new();
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری کلیدها");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void OpenAddModal()
    {
        createCommand = new CreateApiKeyCommand { ExpireAt = DateTime.Now.AddYears(1) };
        expireAtPersian = PersianDateHelper.ToPersianDate(createCommand.ExpireAt);
        newlyCreatedKey = string.Empty;
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        newlyCreatedKey = string.Empty;
    }

    private async Task HandleCreate()
    {
        var parsedDate = PersianDateHelper.FromPersianDate(expireAtPersian);
        if (parsedDate == null)
        {
            NotificationService.ShowError("تاریخ انقضا معتبر نیست");
            return;
        }
        createCommand.ExpireAt = parsedDate.Value;
        isSaving = true;
        try
        {
            var result = await ApiClient.CreateApiKeyAsync(createCommand);
            if (result != null && result.Success)
            {
                newlyCreatedKey = result.GeneratedKey ?? string.Empty;
                await LoadKeys();
            }
            else
            {
                NotificationService.ShowError(result?.Message ?? "خطا در ایجاد کلید");
            }
        }
        catch
        {
            NotificationService.ShowError("خطا در اتصال به سرور");
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task CopyKey()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", newlyCreatedKey);
        NotificationService.ShowSuccess("کلید کپی شد");
    }

    private void ConfirmDelete(ApiKey key) => deleteTarget = key;

    private async Task ExecuteDelete()
    {
        if (deleteTarget == null) return;
        isSaving = true;
        try
        {
            var (success, message) = await ApiClient.DeleteApiKeyAsync(deleteTarget.Id);
            if (success)
            {
                NotificationService.ShowSuccess(message);
                deleteTarget = null;
                await LoadKeys();
            }
            else
            {
                NotificationService.ShowError(message);
            }
        }
        catch
        {
            NotificationService.ShowError("خطا در اتصال به سرور");
        }
        finally
        {
            isSaving = false;
        }
    }
}

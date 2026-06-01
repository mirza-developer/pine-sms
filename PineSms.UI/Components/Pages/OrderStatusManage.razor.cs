using Microsoft.AspNetCore.Components;
using PineSms.Core.Entities;
using PineSms.Core.Features.Order;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class OrderStatusManage
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private List<OrderStatus> statuses = new();
    private bool isLoading = false;
    private bool isSaving = false;
    private bool showModal = false;
    private UpsertOrderStatusCommand editCommand = new();
    private OrderStatus? deleteTarget = null;

    protected override async Task OnInitializedAsync() => await LoadStatuses();

    private async Task LoadStatuses()
    {
        isLoading = true;
        try
        {
            var result = await ApiClient.GetOrderStatusesAsync();
            statuses = result ?? new();
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری وضعیت‌ها");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void OpenAddModal()
    {
        editCommand = new UpsertOrderStatusCommand();
        showModal = true;
    }

    private void OpenEditModal(OrderStatus status)
    {
        editCommand = new UpsertOrderStatusCommand
        {
            Id = status.Id,
            Code = status.Code,
            Title = status.Title
        };
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        editCommand = new UpsertOrderStatusCommand();
    }

    private async Task HandleSave()
    {
        isSaving = true;
        try
        {
            var result = await ApiClient.UpsertOrderStatusAsync(editCommand);
            if (result.Success)
            {
                NotificationService.ShowSuccess(result.Message);
                CloseModal();
                await LoadStatuses();
            }
            else
            {
                NotificationService.ShowError(result.Message);
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

    private void ConfirmDelete(OrderStatus status) => deleteTarget = status;

    private async Task ExecuteDelete()
    {
        if (deleteTarget == null) return;
        isSaving = true;
        try
        {
            var result = await ApiClient.DeleteOrderStatusAsync(deleteTarget.Id);
            if (result.Success)
            {
                NotificationService.ShowSuccess(result.Message);
                deleteTarget = null;
                await LoadStatuses();
            }
            else
            {
                NotificationService.ShowError(result.Message);
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

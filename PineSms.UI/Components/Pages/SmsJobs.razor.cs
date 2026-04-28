using Microsoft.AspNetCore.Components;
using PineSms.Core.Features.Sms;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class SmsJobs
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private List<SmsSendJobDto> jobs = new();
    private bool isLoading = false;

    protected override async Task OnInitializedAsync() => await LoadJobs();

    private async Task LoadJobs()
    {
        isLoading = true;
        try
        {
            var result = await ApiClient.GetSmsJobsAsync();
            jobs = result ?? new();
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری گزارشات");
        }
        finally
        {
            isLoading = false;
        }
    }

    private static string GetBadgeClass(string status) => status switch
    {
        "Completed" => "bg-success",
        "Sending"   => "bg-warning text-dark",
        "Failed"    => "bg-danger",
        _           => "bg-secondary"
    };

    private static string TranslateStatus(string status) => status switch
    {
        "Pending"   => "در انتظار",
        "Sending"   => "در حال ارسال",
        "Completed" => "ارسال شد",
        "Failed"    => "خطا",
        _           => status
    };
}

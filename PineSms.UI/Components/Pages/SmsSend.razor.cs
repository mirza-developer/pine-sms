using Microsoft.AspNetCore.Components;
using PineSms.Core.Entities;
using PineSms.Core.Features.Sms;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class SmsSend
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private AuthStateService AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    // --- customer list state ---
    private List<Customer> customers = new();
    private HashSet<int> selectedIds = new();
    private string selectedRange = "LastMonth";
    private DateTime customFrom = DateTime.Today.AddMonths(-1);
    private DateTime customTo = DateTime.Today;
    private string customFromStr = PersianDateHelper.ToPersianDate(DateTime.Today.AddMonths(-1));
    private string customToStr = PersianDateHelper.ToPersianDate(DateTime.Today);
    private bool isLoadingCustomers = false;

    // --- client-side filters (pending — bound to UI inputs) ---
    private string phoneFilter = string.Empty;
    private bool showTestersOnly = false;

    // --- applied filters (what FilteredCustomers actually uses) ---
    private string appliedPhoneFilter = string.Empty;
    private bool appliedShowTestersOnly = false;

    private IEnumerable<Customer> FilteredCustomers => customers
        .Where(c => (string.IsNullOrEmpty(appliedPhoneFilter) || c.PhoneNumber.Contains(appliedPhoneFilter))
                 && (!appliedShowTestersOnly || c.IsTester));

    // --- SMS settings state ---
    private string fromNumber = string.Empty;
    private string messageText = string.Empty;
    private string alertMessage = string.Empty;
    private string alertClass = "alert-info";
    private bool isSending = false;

    // --- scheduling state ---
    private bool scheduleEnabled = false;
    private int numberOfParts = 2;
    private int delayMinutes = 60;
    private string? firstSendDateStr;
    private TimeOnly firstSendTime = TimeOnly.FromDateTime(DateTime.Now);
    private bool isScheduling = false;
    private List<SmsSendJobDto> recentJobs = new();

    private bool allSelected
    {
        get
        {
            var filtered = FilteredCustomers.ToList();
            return filtered.Count > 0 && filtered.All(c => selectedIds.Contains(c.Id));
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadRecentJobs();
    }

    // ---- date filter helpers ----

    private void OnCustomFromChanged(string? val)
    {
        customFromStr = val ?? string.Empty;
        customFrom = PersianDateHelper.FromPersianDate(val) ?? DateTime.Today.AddMonths(-1);
    }

    private void OnCustomToChanged(string? val)
    {
        customToStr = val ?? string.Empty;
        customTo = PersianDateHelper.FromPersianDate(val) ?? DateTime.Today;
    }

    private void OnRangeChanged()
    {
        if (selectedRange != "Custom")
        {
            customFrom = PersianDateHelper.GetDateRangeFrom(selectedRange);
            customTo = DateTime.Now;
        }
    }

    // ---- unified filter + load ----

    private async Task ApplyFiltersAndLoad()
    {
        appliedPhoneFilter = phoneFilter;
        appliedShowTestersOnly = showTestersOnly;
        await LoadCustomers();
    }

    // ---- customer loading ----

    private async Task LoadCustomers()
    {
        isLoadingCustomers = true;
        selectedIds.Clear();
        try
        {
            var from = selectedRange == "Custom" ? customFrom : PersianDateHelper.GetDateRangeFrom(selectedRange);
            var to = selectedRange == "Custom" ? customTo : DateTime.Now;
            var result = await ApiClient.GetCustomersByRangeAsync(from, to);
            customers = result ?? new List<Customer>();
            selectedIds = customers.Select(c => c.Id).ToHashSet();
            if (customers.Count == 0)
                NotificationService.ShowInformation("هیچ مشتری در این بازه زمانی یافت نشد");
            else
                NotificationService.ShowSuccess($"{customers.Count} مشتری یافت شد");
        }
        catch
        {
            NotificationService.ShowError("خطا در بارگذاری مشتریان");
            alertClass = "alert-danger";
            alertMessage = "خطا در بارگذاری مشتریان";
        }
        finally
        {
            isLoadingCustomers = false;
        }
    }

    // ---- selection helpers ----

    private void ToggleSelection(int id, bool selected)
    {
        if (selected) selectedIds.Add(id);
        else selectedIds.Remove(id);
    }

    private void ToggleRow(int id)
    {
        if (selectedIds.Contains(id)) selectedIds.Remove(id);
        else selectedIds.Add(id);
    }

    private void ToggleAll(Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        bool check = (bool)(e.Value ?? false);
        if (check)
            foreach (var c in FilteredCustomers) selectedIds.Add(c.Id);
        else
            foreach (var c in FilteredCustomers) selectedIds.Remove(c.Id);
    }

    // ---- instant send ----

    private async Task HandleSendSms()
    {
        if (!ValidateForm()) return;
        isSending = true;
        try
        {
            var command = new SendSmsCommand
            {
                CustomerIds = selectedIds.ToList(),
                MessageText = messageText,
                FromNumber = fromNumber,
                DateRangeType = selectedRange,
                CustomFrom = selectedRange == "Custom" ? customFrom : null,
                CustomTo = selectedRange == "Custom" ? customTo : null
            };
            var result = await ApiClient.SendSmsAsync(command);
            if (result?.Success == true)
                NotificationService.ShowSuccess(result.Message);
            else
                NotificationService.ShowError(result?.Message ?? "خطا در ارسال");
            alertClass = result?.Success == true ? "alert-success" : "alert-danger";
            alertMessage = result?.Message ?? "خطا در ارسال";
        }
        catch
        {
            NotificationService.ShowError("خطا در اتصال به سرور");
            alertClass = "alert-danger";
            alertMessage = "خطا در اتصال به سرور";
        }
        finally
        {
            isSending = false;
        }
    }

    // ---- scheduled send ----

    private async Task HandleScheduleSms()
    {
        if (!ValidateForm()) return;
        isScheduling = true;
        try
        {
            DateTime? firstSendAt = null;
            if (!string.IsNullOrEmpty(firstSendDateStr))
            {
                var date = PersianDateHelper.FromPersianDate(firstSendDateStr);
                if (date.HasValue)
                    firstSendAt = date.Value.Date.Add(firstSendTime.ToTimeSpan());
            }

            var command = new ScheduleSmsCommand
            {
                CustomerIds = selectedIds.ToList(),
                MessageText = messageText,
                FromNumber = fromNumber,
                NumberOfParts = numberOfParts,
                DelayMinutesBetweenParts = delayMinutes,
                FirstSendAt = firstSendAt
            };
            var result = await ApiClient.ScheduleSmsAsync(command);
            if (result?.Success == true)
            {
                NotificationService.ShowSuccess(result.Message);
                alertClass = "alert-success";
                alertMessage = result.Message;
                await LoadRecentJobs();
            }
            else
            {
                NotificationService.ShowError(result?.Message ?? "خطا در زمان‌بندی");
                alertClass = "alert-danger";
                alertMessage = result?.Message ?? "خطا در زمان‌بندی";
            }
        }
        catch
        {
            NotificationService.ShowError("خطا در اتصال به سرور");
            alertClass = "alert-danger";
            alertMessage = "خطا در اتصال به سرور";
        }
        finally
        {
            isScheduling = false;
        }
    }

    private async Task LoadRecentJobs()
    {
        try
        {
            var jobs = await ApiClient.GetSmsJobsAsync();
            recentJobs = jobs?.Take(5).ToList() ?? new();
        }
        catch { /* silently ignore */ }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrEmpty(messageText))
        {
            alertClass = "alert-warning";
            alertMessage = "لطفاً متن پیامک را وارد کنید";
            return false;
        }
        if (string.IsNullOrEmpty(fromNumber))
        {
            alertClass = "alert-warning";
            alertMessage = "لطفاً شماره فرستنده را وارد کنید";
            return false;
        }
        return true;
    }

    // ---- job badge helpers ----

    private static string GetPartBadgeClass(string status) => status switch
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

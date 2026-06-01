using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PineSms.Core.Features.Order;
using PineSms.UI.Features.TrackingImport;
using PineSms.UI.Services;

namespace PineSms.UI.Components.Pages;

public partial class AnanasTrackingImport
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private List<TrackingEntry> entries = new();
    private List<string> notFoundCodes = new();
    private string alertMessage = string.Empty;
    private string alertClass = "alert-info";
    private bool isLoading = false;

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        entries.Clear();
        ClearAlert();
        try
        {
            using var stream = e.File.OpenReadStream(maxAllowedSize: 52_428_800);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            ReadFromExcel(ms);

            if (entries.Count == 0)
            {
                NotificationService.ShowWarning("فایل خالی است یا داده‌ای یافت نشد");
                return;
            }
            NotificationService.ShowInformation($"{entries.Count} ردیف از فایل خوانده شد");
        }
        catch (Exception ex)
        {
            NotificationService.ShowError($"خطا در خواندن فایل: {ex.Message}");
        }
    }

    private void ReadFromExcel(MemoryStream ms)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(ms);

        // Read header row and locate columns by name (trim whitespace)
        if (!reader.Read()) return;

        int barcodeColIdx = -1;
        int orderCodeColIdx = -1;
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var header = reader.GetValue(i)?.ToString()?.Trim();
            if (header == "بارکد") barcodeColIdx = i;
            else if (header == "کدفاکتور") orderCodeColIdx = i;
        }

        if (barcodeColIdx < 0 || orderCodeColIdx < 0)
        {
            NotificationService.ShowError("ستون‌های «بارکد» یا «کدفاکتور» در فایل یافت نشد");
            return;
        }

        // Collect all data rows
        var rows = new List<TrackingDataRow>();
        while (reader.Read())
        {
            var orderCodeRaw = reader.GetValue(orderCodeColIdx)?.ToString()?.Trim();
            var barcodeRaw = reader.GetValue(barcodeColIdx)?.ToString()?.Trim();

            if (!string.IsNullOrEmpty(orderCodeRaw) && !string.IsNullOrEmpty(barcodeRaw))
                rows.Add(new TrackingDataRow { OrderCode = orderCodeRaw, Barcode = barcodeRaw });
        }

        // Drop the last row (جمع کل summary row)
        if (rows.Count > 0)
            rows.RemoveAt(rows.Count - 1);

        foreach (var row in rows)
        {
            entries.Add(new TrackingEntry
            {
                OrderCode = row.OrderCode,
                PostalTrackingCode = row.Barcode
            });
        }
    }

    private async Task HandleImport()
    {
        isLoading = true;
        try
        {
            var command = new BulkUpdateTrackingCommand { Entries = entries };
            var result = await ApiClient.BulkUpdateTrackingAsync(command);
            if (result != null)
            {
                alertClass = result.NotFoundCount == 0 ? "alert-success" : "alert-warning";
                alertMessage = result.Message;
                notFoundCodes = result.NotFoundCodes;
                if (result.UpdatedCount > 0)
                {
                    NotificationService.ShowSuccess(result.Message);
                    entries.Clear();
                }
            }
        }
        catch
        {
            NotificationService.ShowError("خطا در اتصال به سرور");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ClearFile() { entries.Clear(); ClearAlert(); }

    private void ClearAlert()
    {
        alertMessage = string.Empty;
        notFoundCodes.Clear();
    }
}

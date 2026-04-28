using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PineSms.Core.Features.Customer;
using PineSms.UI.Services;
using System.Text.RegularExpressions;

namespace PineSms.UI.Components.Pages;

public partial class CustomerImport
{
    [Inject] private ApiClientService ApiClient { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private List<string> phoneNumbers = new();
    private List<string> invalidNumbers = new();
    private List<string> duplicateNumbers = new();
    private string alertMessage = string.Empty;
    private string alertClass = "alert-info";
    private bool isLoading = false;
    private bool showIgnoreButton = false;
    private string? saveDate;

    // Matches a 10-digit Iranian mobile number starting with 9 (no country code, no leading 0)
    private static readonly Regex PhonePattern =
        new(@"^9\d{9}$", RegexOptions.Compiled);

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        phoneNumbers.Clear();
        ClearAlert();
        try
        {
            var fileName = e.File.Name.ToLowerInvariant();
            using var stream = e.File.OpenReadStream(maxAllowedSize: 52_428_800); // 50 MB
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            if (fileName.EndsWith(".txt"))
            {
                ReadFromTxt(ms);
            }
            else
            {
                // Peek at the first bytes to decide the format before trying ExcelDataReader
                bool isHtml = StartsWithHtml(ms);
                ms.Position = 0;

                if (isHtml)
                {
                    ReadFromHtml(ms);
                }
                else
                {
                    try
                    {
                        ReadFromExcel(ms);
                    }
                    catch (Exception)
                    {
                        // Not a recognised binary/OOXML Excel format; try plain-text/CSV
                        ms.Position = 0;
                        ReadFromCsv(ms);
                    }
                }
            }

            if (phoneNumbers.Count == 0)
            {
                NotificationService.ShowWarning("فایل خالی است یا شماره‌ای یافت نشد");
                return;
            }
            NotificationService.ShowInformation($"{phoneNumbers.Count} شماره از فایل خوانده شد");
        }
        catch (Exception ex)
        {
            NotificationService.ShowError($"خطا در خواندن فایل: {ex.Message}");
        }
    }

    /// <summary>Checks whether the stream content begins with an HTML tag.</summary>
    private static bool StartsWithHtml(MemoryStream ms)
    {
        // Read up to 512 bytes to detect an HTML signature
        var buf = new byte[Math.Min(512, ms.Length)];
        int read = ms.Read(buf, 0, buf.Length);
        var prefix = System.Text.Encoding.UTF8.GetString(buf, 0, read).TrimStart();
        return prefix.StartsWith("<", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads phone numbers from a real .xls or .xlsx binary file.</summary>
    private void ReadFromExcel(MemoryStream ms)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(ms);
        // Skip header row
        if (!reader.Read()) return;
        while (reader.Read())
        {
            var val = reader.GetValue(0)?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(val))
                phoneNumbers.Add(val);
        }
    }

    /// <summary>
    /// Reads phone numbers from an HTML table (e.g. a web-exported .xls file).
    /// Scans every &lt;td&gt; cell in every data row and collects values that
    /// match the Iranian mobile number pattern (10 digits, starts with 9).
    /// </summary>
    private void ReadFromHtml(MemoryStream ms)
    {
        using var sr = new StreamReader(ms, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var html = sr.ReadToEnd();

        // Extract all <tr> blocks (skip the first row which is the header)
        var rowMatches = Regex.Matches(
            html, @"<tr[^>]*>(.*?)</tr>",
            RegexOptions.Singleline |
            RegexOptions.IgnoreCase);

        bool headerSkipped = false;
        foreach (Match row in rowMatches)
        {
            // Skip the header row (contains <th> tags)
            if (!headerSkipped)
            {
                if (row.Value.IndexOf("<th", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    headerSkipped = true;
                    continue;
                }
            }

            // Extract every <td> cell value in this row
            var cellMatches = Regex.Matches(
                row.Groups[1].Value, @"<td[^>]*>(.*?)</td>",
                RegexOptions.Singleline |
                RegexOptions.IgnoreCase);

            foreach (Match cell in cellMatches)
            {
                // Strip any inner HTML tags and decode basic entities
                var raw = Regex.Replace(cell.Groups[1].Value, "<[^>]+>", "").Trim();
                raw = System.Net.WebUtility.HtmlDecode(raw);
                if (PhonePattern.IsMatch(raw))
                {
                    phoneNumbers.Add(raw);
                    break; // take only the first matching phone per row to avoid duplicates
                }
            }
        }
    }

    /// <summary>Reads phone numbers from a CSV or tab-separated text file.</summary>
    private void ReadFromCsv(MemoryStream ms)
    {
        using var sr = new StreamReader(ms, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        // Skip header row
        if (sr.ReadLine() == null) return;
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // Support comma, semicolon, tab, or pipe as delimiters; take first column
            var val = line.Split(',', ';', '\t', '|')[0].Trim().Trim('"');
            if (!string.IsNullOrEmpty(val))
                phoneNumbers.Add(val);
        }
    }

    /// <summary>Reads phone numbers from a plain .txt file where each line is one number.</summary>
    private void ReadFromTxt(MemoryStream ms)
    {
        using var sr = new StreamReader(ms, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            var val = line.Trim();
            if (!string.IsNullOrEmpty(val))
                phoneNumbers.Add(val);
        }
    }

    private async Task HandleImport() => await ImportAsync(false);
    private async Task HandleIgnoreAndImport() => await ImportAsync(true);

    private async Task ImportAsync(bool ignoreInvalid)
    {
        isLoading = true;
        showIgnoreButton = false;
        try
        {
            var command = new ImportCustomersCommand
            {
                PhoneNumbers = phoneNumbers,
                IgnoreInvalid = ignoreInvalid,
                SaveDate = saveDate
            };
            var result = await ApiClient.ImportCustomersAsync(command);
            if (result != null)
            {
                alertClass = result.Success ? "alert-success" : "alert-warning";
                alertMessage = result.Message;
                invalidNumbers = result.InvalidNumbers;
                duplicateNumbers = result.DuplicateNumbers;
                showIgnoreButton = !result.Success && (result.InvalidNumbers.Count > 0 || result.DuplicateNumbers.Count > 0);
                if (result.Success)
                {
                    phoneNumbers.Clear();
                    NotificationService.ShowSuccess(result.Message);
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

    private void ClearFile() { phoneNumbers.Clear(); ClearAlert(); }
    private void ClearAlert()
    {
        alertMessage = string.Empty;
        invalidNumbers.Clear();
        duplicateNumbers.Clear();
        showIgnoreButton = false;
    }
}

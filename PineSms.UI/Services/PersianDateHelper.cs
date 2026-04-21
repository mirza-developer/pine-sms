using System.Globalization;

namespace PineSms.UI.Services;

public static class PersianDateHelper
{
    private static readonly PersianCalendar pc = new();

    public static string ToPersianDate(DateTime date)
    {
        try
        {
            int year = pc.GetYear(date);
            int month = pc.GetMonth(date);
            int day = pc.GetDayOfMonth(date);
            return $"{year}/{month:D2}/{day:D2}";
        }
        catch
        {
            return date.ToString("yyyy/MM/dd");
        }
    }

    public static DateTime GetStartOfToday() => DateTime.Now.Date;

    /// <summary>Converts a Persian date string (yyyy/MM/dd) to UTC DateTime. Returns null if parsing fails.</summary>
    public static DateTime? FromPersianDate(string? persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate)) return null;
        var parts = persianDate.Split('/');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out int y) &&
            int.TryParse(parts[1], out int m) &&
            int.TryParse(parts[2], out int d))
        {
            try { return pc.ToDateTime(y, m, d, 0, 0, 0, 0); }
            catch { return null; }
        }
        return null;
    }

    public static DateTime GetDateRangeFrom(string rangeType)
    {
        return rangeType switch
        {
            "LastWeek" => DateTime.Now.AddDays(-7),
            "LastTwoWeeks" => DateTime.Now.AddDays(-14),
            "LastMonth" => DateTime.Now.AddMonths(-1),
            "LastSeason" => DateTime.Now.AddMonths(-3),
            "LastYear" => DateTime.Now.AddYears(-1),
            _ => DateTime.Now.AddDays(-30)
        };
    }
}

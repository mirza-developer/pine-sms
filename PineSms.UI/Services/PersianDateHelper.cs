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

    public static DateTime GetStartOfToday() => DateTime.UtcNow.Date;

    public static DateTime GetDateRangeFrom(string rangeType)
    {
        return rangeType switch
        {
            "LastWeek" => DateTime.UtcNow.AddDays(-7),
            "LastTwoWeeks" => DateTime.UtcNow.AddDays(-14),
            "LastMonth" => DateTime.UtcNow.AddMonths(-1),
            "LastSeason" => DateTime.UtcNow.AddMonths(-3),
            "LastYear" => DateTime.UtcNow.AddYears(-1),
            _ => DateTime.UtcNow.AddDays(-30)
        };
    }
}

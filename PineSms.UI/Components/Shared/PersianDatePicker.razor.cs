using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace PineSms.UI.Components.Shared;

public partial class PersianDatePicker
{
    private enum DatePickerMode { Days, Months, Years }

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    private bool isOpen = false;
    private int currentYear;
    private int currentMonth;
    private int yearsViewStart;
    private DatePickerMode currentMode = DatePickerMode.Days;
    private List<CalendarCell> calendarCells = new();

    private static readonly PersianCalendar pc = new();

    private static readonly string[] PersianMonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    private static readonly string[] DayNames = { "ش", "ی", "د", "س", "چ", "پ", "ج" };

    protected override void OnInitialized()
    {
        var now = DateTime.Now;
        if (!string.IsNullOrEmpty(Value))
        {
            var parts = Value.Split('/');
            if (parts.Length == 3 && int.TryParse(parts[0], out int y) &&
                int.TryParse(parts[1], out int m) && int.TryParse(parts[2], out _))
            {
                currentYear = y;
                currentMonth = m;
            }
            else
            {
                currentYear = pc.GetYear(now);
                currentMonth = pc.GetMonth(now);
            }
        }
        else
        {
            currentYear = pc.GetYear(now);
            currentMonth = pc.GetMonth(now);
        }
        yearsViewStart = currentYear - (currentYear % 12);
        BuildCalendar();
    }

    private void ToggleCalendar() => isOpen = !isOpen;
    private void CloseCalendar() => isOpen = false;

    private void SwitchToMonths()
    {
        currentMode = DatePickerMode.Months;
    }

    private void SwitchToYears()
    {
        yearsViewStart = currentYear - (currentYear % 12);
        currentMode = DatePickerMode.Years;
    }

    private void NavigatePrev()
    {
        if (currentMode == DatePickerMode.Days)
        {
            currentMonth--;
            if (currentMonth < 1) { currentMonth = 12; currentYear--; }
            BuildCalendar();
        }
        else if (currentMode == DatePickerMode.Months)
        {
            currentYear--;
        }
        else
        {
            yearsViewStart -= 12;
        }
    }

    private void NavigateNext()
    {
        if (currentMode == DatePickerMode.Days)
        {
            currentMonth++;
            if (currentMonth > 12) { currentMonth = 1; currentYear++; }
            BuildCalendar();
        }
        else if (currentMode == DatePickerMode.Months)
        {
            currentYear++;
        }
        else
        {
            yearsViewStart += 12;
        }
    }

    private void SelectMonth(int month)
    {
        currentMonth = month;
        currentMode = DatePickerMode.Days;
        BuildCalendar();
    }

    private void SelectYear(int year)
    {
        currentYear = year;
        currentMode = DatePickerMode.Months;
    }

    private void BuildCalendar()
    {
        calendarCells.Clear();
        var firstDay = pc.ToDateTime(currentYear, currentMonth, 1, 0, 0, 0, 0);
        int dayOfWeek = (int)firstDay.DayOfWeek; // 0=Sun
        // Persian week starts on Saturday (6)
        int startOffset = (dayOfWeek + 1) % 7; // adjust so Saturday=0

        int daysInMonth = pc.GetDaysInMonth(currentYear, currentMonth);

        // Previous month padding
        if (startOffset > 0)
        {
            int prevMonth = currentMonth - 1;
            int prevYear = currentYear;
            if (prevMonth < 1) { prevMonth = 12; prevYear--; }
            int prevDays = pc.GetDaysInMonth(prevYear, prevMonth);
            for (int i = startOffset - 1; i >= 0; i--)
                calendarCells.Add(new CalendarCell { Day = prevDays - i, Month = prevMonth, Year = prevYear, IsCurrentMonth = false });
        }

        // Current month days
        for (int d = 1; d <= daysInMonth; d++)
            calendarCells.Add(new CalendarCell { Day = d, Month = currentMonth, Year = currentYear, IsCurrentMonth = true });

        // Fill to 42 cells
        int nextMonth = currentMonth + 1;
        int nextYear = currentYear;
        if (nextMonth > 12) { nextMonth = 1; nextYear++; }
        int fill = 1;
        while (calendarCells.Count < 42)
            calendarCells.Add(new CalendarCell { Day = fill++, Month = nextMonth, Year = nextYear, IsCurrentMonth = false });
    }

    private string GetCellClass(CalendarCell cell)
    {
        var classes = new List<string>();
        if (!cell.IsCurrentMonth) classes.Add("other-month");

        var today = DateTime.Now;
        if (cell.Year == pc.GetYear(today) && cell.Month == pc.GetMonth(today) && cell.Day == pc.GetDayOfMonth(today))
            classes.Add("today");

        if (!string.IsNullOrEmpty(Value))
        {
            var selectedDate = $"{cell.Year:D4}/{cell.Month:D2}/{cell.Day:D2}";
            if (selectedDate == Value) classes.Add("selected");
        }

        return string.Join(" ", classes);
    }

    private string GetMonthClass(int month)
    {
        var classes = new List<string>();
        var today = DateTime.Now;
        if (currentYear == pc.GetYear(today) && month == pc.GetMonth(today))
            classes.Add("today");
        if (!string.IsNullOrEmpty(Value))
        {
            var parts = Value.Split('/');
            if (parts.Length == 3 && int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
                if (y == currentYear && m == month) classes.Add("selected");
        }
        return string.Join(" ", classes);
    }

    private string GetYearClass(int year)
    {
        var classes = new List<string>();
        var today = DateTime.Now;
        if (year == pc.GetYear(today))
            classes.Add("today");
        if (!string.IsNullOrEmpty(Value))
        {
            var parts = Value.Split('/');
            if (parts.Length == 3 && int.TryParse(parts[0], out int y))
                if (y == year) classes.Add("selected");
        }
        return string.Join(" ", classes);
    }

    private async Task SelectDate(CalendarCell cell)
    {
        if (!cell.IsCurrentMonth)
        {
            currentYear = cell.Year;
            currentMonth = cell.Month;
            BuildCalendar();
            return;
        }
        var dateStr = $"{cell.Year:D4}/{cell.Month:D2}/{cell.Day:D2}";
        Value = dateStr;
        await ValueChanged.InvokeAsync(dateStr);
        isOpen = false;
    }

    private class CalendarCell
    {
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public bool IsCurrentMonth { get; set; }
    }
}

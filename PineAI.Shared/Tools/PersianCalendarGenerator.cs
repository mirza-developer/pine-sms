namespace PineAI.Shared;
public class PersianCalendarGenerator
{
    public PersianYear CreateYearCalendar(int year)
    {
        var allDaysOfYear = new List<PersianDay>();
        var allWeeksOfYear = new List<PersianWeek>();
        var allMonthsOfYear = new List<PersianMonth>();
        CreateAllDaysInYear();
        CreateAllWeeksInYear();
        AssignWeeksToMonths();
        PersianYear currentYear;
        AssignMonthsToYear();
        return currentYear;
        void CreateAllDaysInYear()
        {
            var firstDayOfYear = new PersianDay
            {
                GregorianDay = PersianCalendarTools.PersianToGregorian(year + "/01/01"),
            };
            var lastDayOfYear = new PersianDay();
            lastDayOfYear.GregorianDay = PersianCalendarTools.PersianIsLeap(year)
                ? PersianCalendarTools.PersianToGregorian(year + "/12/30")
                : PersianCalendarTools.PersianToGregorian(year + "/12/29");
            var tempDay = firstDayOfYear;
            do
            {
                allDaysOfYear.Add(tempDay);
                tempDay.GregorianDay = tempDay.GregorianDay.AddDays(1);
                tempDay = new PersianDay
                {
                    GregorianDay = tempDay.GregorianDay,
                    FullDate = PersianCalendarTools.GregorianToPersian(tempDay.GregorianDay),
                    IsHoliday = (tempDay.GregorianDay.DayOfWeek == DayOfWeek.Friday),
                };
            } while (tempDay.GregorianDay <= lastDayOfYear.GregorianDay);
        }
        void CreateAllWeeksInYear()
        {
            var monthWeekCounter = 0;
            for (var i = 0; i < allDaysOfYear.Count; i++)
            {
                if (i == 0 | allDaysOfYear[i].GregorianDay.DayOfWeek == DayOfWeek.Saturday)
                {
                    monthWeekCounter = 1;
                    var week = new PersianWeek
                    {
                        ListDays = new List<PersianDay>(),
                        WeekNumber = monthWeekCounter
                    };
                    allWeeksOfYear.Add(week);
                }
                var currentWeek = allWeeksOfYear[allWeeksOfYear.Count - 1];
                allDaysOfYear[i].PersianWeek = currentWeek;
                allWeeksOfYear[allWeeksOfYear.Count - 1].ListDays.Add(allDaysOfYear[i]);
                monthWeekCounter++;
            }
        }
        void AssignWeeksToMonths()
        {
            allMonthsOfYear.Add(new PersianMonth
            {
                ListWeeks = new List<PersianWeek>(),
                MonthNumber = 1
            });
            var debug = 0;
            foreach (var week in allWeeksOfYear)
            {
                allMonthsOfYear[allMonthsOfYear.Count - 1].ListWeeks.Add(new PersianWeek
                {
                    ListDays = new List<PersianDay>(),
                    WeekNumber = allMonthsOfYear[allMonthsOfYear.Count - 1].ListWeeks.Count
                });
                foreach (var day in week.ListDays)
                {
                    var convertedToPersian = PersianCalendarTools.GregorianToPersian(day.GregorianDay).Split('/');
                    if (convertedToPersian[2] == "01")
                    {
                        if (convertedToPersian[1] != "01")
                        {
                            allMonthsOfYear.Add(new PersianMonth
                            {
                                Year = year,
                                ListWeeks = new List<PersianWeek>(),
                                MonthNumber = allMonthsOfYear[allMonthsOfYear.Count - 1].MonthNumber + 1
                            });
                        }

                        allMonthsOfYear[allMonthsOfYear.Count - 1].ListWeeks.Add(new PersianWeek
                        {
                            ListDays = new List<PersianDay>(),
                            WeekNumber = allMonthsOfYear[allMonthsOfYear.Count - 1].ListWeeks.Count
                        });
                    }

                    allMonthsOfYear[allMonthsOfYear.Count - 1].ListWeeks[allMonthsOfYear[allMonthsOfYear.Count - 1].ListWeeks.Count - 1].ListDays.Add(day);
                    debug++;
                }
            }

            allMonthsOfYear[0].ListWeeks.RemoveAt(0);
            allMonthsOfYear[0].ListWeeks.RemoveAt(allMonthsOfYear[0].ListWeeks.Count - 1);
        }
        void AssignMonthsToYear()
        {
            currentYear = new PersianYear
            {
                listMonths = allMonthsOfYear,
                YearNumber = year
            };
            var emptyList = new List<int>();
            for (var i = 0; i < currentYear.listMonths[0].ListWeeks.Count; i++)
            {
                var week = currentYear.listMonths[0].ListWeeks[i];
                if (week.ListDays.Count == 0)
                {
                    emptyList.Add(i);
                }
            }

            var counter = 0;
            foreach (var val in emptyList)
            {
                currentYear.listMonths[0].ListWeeks.RemoveAt(val - counter);
                counter++;
            }

        }
    }
    public PersianMonth GetMonth(int year, int month)
    {
        var yearEntity = CreateYearCalendar(year);
        var monthEntity = new PersianMonth();
        for (var i = 0; i < yearEntity.listMonths.Count; i++)
        {
            if (i + 1 != month)
                continue;
            monthEntity = yearEntity.listMonths[i];
            break;
        }
        return monthEntity;
    }
    private PersianDay GetLastDayOfMonth(PersianMonth PersianMonth)
    {
        for (var i = PersianMonth.ListWeeks.Count - 1; i >= 0; i--)
            if (PersianMonth.ListWeeks[i].ListDays.Count != 0)
                return PersianMonth.ListWeeks[i].ListDays[PersianMonth.ListWeeks[i].ListDays.Count - 1];
        return null;
    }
    private PersianDay GetFirstDayOfMonth(PersianMonth PersianMonth) => PersianMonth.ListWeeks
        .Where(t => t.ListDays.Count != 0)
        .Select(t => t.ListDays[0]).FirstOrDefault();
    private PersianDay GetFirstDayOfYear(int year)
    {
        var yearCalendar = CreateYearCalendar(year);
        return GetFirstDayOfMonth(yearCalendar.listMonths[0]);
    }
    private PersianDay GetLastDayOfYear(int year)
    {
        var yearCalendar = CreateYearCalendar(year);
        return GetLastDayOfMonth(yearCalendar.listMonths[11]);
    }
}

public class PersianDay
{
    public PersianDay()
    {
        IsHoliday = false;
    }
    public PersianWeek PersianWeek { get; set; }
    public bool IsHoliday { get; set; }
    public DateTime GregorianDay { get; set; }
    public string FullDate { get; set; }
}

public class PersianMonth
{
    public int Year { get; set; }
    public int MonthNumber { get; set; }
    public List<PersianWeek> ListWeeks { get; set; }

}

public class PersianWeek
{
    public int WeekNumber { get; set; }
    public List<PersianDay> ListDays { get; set; }
    public PersianMonth PersianMonth { get; set; }
}

public class PersianYear
{
    public List<PersianMonth> listMonths { get; set; }
    public int YearNumber { get; set; }
}


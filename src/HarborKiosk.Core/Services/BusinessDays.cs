namespace HarborKiosk.Core.Services;

public static class BusinessDays
{
    public static DateOnly Add(DateOnly start, int businessDays)
    {
        var date = start;
        while (businessDays > 0)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                businessDays--;
            }
        }

        return date;
    }
}

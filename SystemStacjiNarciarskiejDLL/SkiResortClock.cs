namespace SystemStacjiNarciarskiejDLL;

public static class SkiResortClock
{
    private static readonly TimeZoneInfo ResortTimeZone = LoadResortTimeZone();

    public static DateTime Now =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ResortTimeZone), DateTimeKind.Unspecified);

    public static DateTime Today => Now.Date;

    public static DateTime StartOfDay(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

    private static TimeZoneInfo LoadResortTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
    }
}

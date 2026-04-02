namespace landing_page_isis.Extensions;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo PortoVelhoZone = TimeZoneInfo.FindSystemTimeZoneById("America/Porto_Velho");

    public static DateTime ToPortoVelhoTime(this DateTime date)
    {
        if (date.Kind == DateTimeKind.Local)
            date = date.ToUniversalTime();

        return TimeZoneInfo.ConvertTimeFromUtc(date, PortoVelhoZone);
    }

    public static DateTime ToPortoVelhoTime(this DateTimeOffset dateOffset)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(dateOffset.UtcDateTime, PortoVelhoZone);
    }
}

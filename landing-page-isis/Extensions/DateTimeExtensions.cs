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

    public static DateTimeOffset ToPortoVelhoDateTimeOffset(this DateTime date)
    {
        var offset = new DateTimeOffset(date, PortoVelhoZone.GetUtcOffset(date));
        return offset.ToUniversalTime(); // Postgres requires Offset 0 (UTC)
    }
}

namespace landing_page_isis.Extensions;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo PortoVelhoZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Porto_Velho"
    );

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
        DateTime utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();

        DateTime portoVelhoTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, PortoVelhoZone);

        var offset = new DateTimeOffset(
            DateTime.SpecifyKind(portoVelhoTime, DateTimeKind.Unspecified),
            PortoVelhoZone.GetUtcOffset(portoVelhoTime)
        );

        return offset.ToUniversalTime();
    }
}

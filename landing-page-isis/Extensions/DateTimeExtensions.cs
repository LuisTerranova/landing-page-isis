namespace landing_page_isis.Extensions;

public static class DateTimeExtensions
{
    private static readonly TimeZoneInfo PortoVelhoZone = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Porto_Velho"
    );

    public static DateTime ToPortoVelhoTime(this DateTime date)
    {
        return TimeZoneInfo.ConvertTime(date, PortoVelhoZone);
    }

    public static DateTime ToPortoVelhoTime(this DateTimeOffset dateOffset)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(dateOffset.UtcDateTime, PortoVelhoZone);
    }

    public static DateTimeOffset ToPortoVelhoDateTimeOffset(this DateTime date)
    {
        var unspecified = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        var offset = PortoVelhoZone.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset).ToUniversalTime();
    }
}

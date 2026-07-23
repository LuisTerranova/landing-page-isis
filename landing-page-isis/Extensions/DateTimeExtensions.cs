namespace landing_page_isis.Extensions;

/// <summary>
/// Provides helper extension methods for converting date and time instances to and from the Porto Velho timezone.
/// </summary>
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

    /// <summary>
    /// Converts a raw local DateTime representing Porto Velho time into a UTC DateTimeOffset.
    /// Crucial for database storage, ensuring the client's local inputs are normalized to UTC.
    /// </summary>
    public static DateTimeOffset ToPortoVelhoDateTimeOffset(this DateTime date)
    {
        // Force the DateTimeKind to unspecified so that GetUtcOffset calculates the offset correctly for Porto Velho timezone
        var unspecified = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        var offset = PortoVelhoZone.GetUtcOffset(unspecified);

        // Build the offset and shift it to UTC representation
        return new DateTimeOffset(unspecified, offset).ToUniversalTime();
    }
}

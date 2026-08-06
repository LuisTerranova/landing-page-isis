using landing_page_isis.Extensions;
using Xunit;

namespace landing_page_isis.tests;

public class DateTimeExtensionsTests
{
    [Fact]
    public void ToPortoVelhoTime_DateTimeOffset_ShouldConvertCorrectlyToUtcMinus4()
    {
        // Porto Velho is UTC-4 year round (no daylight saving time)
        var utcDate = new DateTimeOffset(2026, 8, 5, 14, 0, 0, TimeSpan.Zero); // 14:00 UTC

        var pvhDate = utcDate.ToPortoVelhoTime();

        // 14:00 UTC - 4h = 10:00 Porto Velho
        Assert.Equal(10, pvhDate.Hour);
        Assert.Equal(5, pvhDate.Day);
    }

    [Fact]
    public void ToPortoVelhoDateTimeOffset_LocalTime_ShouldProduceEquivalentUtcOffset()
    {
        // 10:00 local time in Porto Velho
        var localDate = new DateTime(2026, 8, 5, 10, 0, 0);

        var dateTimeOffsetUtc = localDate.ToPortoVelhoDateTimeOffset();

        // Should be stored as 14:00 UTC
        Assert.Equal(14, dateTimeOffsetUtc.UtcDateTime.Hour);
        Assert.Equal(TimeSpan.Zero, dateTimeOffsetUtc.Offset);
    }
}

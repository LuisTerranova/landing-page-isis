using landing_page_isis.Extensions;
using Xunit;

namespace landing_page_isis.tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("69999998888", "(69) 99999-8888")]
    [InlineData("6933334444", "(69) 3333-4444")]
    [InlineData("11988887777", "(11) 98888-7777")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void FormatPhone_ShouldFormatCorrectly(string? input, string? expected)
    {
        var result = input?.FormatPhone();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12345678901", "123.456.789-01")]
    [InlineData("123.456.789-01", "123.456.789-01")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void FormatCpf_ShouldFormatCorrectly(string? input, string expected)
    {
        var result = input.FormatCpf();
        Assert.Equal(expected, result);
    }
}

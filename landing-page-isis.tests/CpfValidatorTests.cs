using landing_page_isis.core.Helpers;

namespace landing_page_isis.tests;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("342.444.896-65")]
    [InlineData("34244489665")]
    [InlineData("111.444.777-35")]
    [InlineData("11144477735")]
    [InlineData("937.073.930-03")]
    [InlineData("93707393003")]
    [InlineData("123.456.789-09")]
    [InlineData("12345678909")]
    public void IsValid_ShouldReturnTrue_ForValidCpf(string cpf)
    {
        Assert.True(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("52998224726")]
    [InlineData("342.444.896-94")]
    [InlineData("34244489694")]
    [InlineData("111.444.777-36")]
    [InlineData("11144477736")]
    [InlineData("937.073.930-08")]
    [InlineData("93707393008")]
    [InlineData("987.654.321-11")]
    [InlineData("98765432111")]
    [InlineData("123.456.789-00")]
    [InlineData("12345678900")]
    [InlineData("000.000.000-01")]
    public void IsValid_ShouldReturnFalse_ForInvalidCpf(string cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData("000.000.000-00")]
    [InlineData("111.111.111-11")]
    [InlineData("222.222.222-22")]
    [InlineData("333.333.333-33")]
    [InlineData("444.444.444-44")]
    [InlineData("555.555.555-55")]
    [InlineData("666.666.666-66")]
    [InlineData("777.777.777-77")]
    [InlineData("888.888.888-88")]
    [InlineData("999.999.999-99")]
    public void IsValid_ShouldReturnFalse_ForRepeatedDigits(string cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForNull()
    {
        Assert.False(CpfValidator.IsValid(null));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForEmptyString()
    {
        Assert.False(CpfValidator.IsValid(string.Empty));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForWhitespace()
    {
        Assert.False(CpfValidator.IsValid("   "));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForWrongLength()
    {
        Assert.False(CpfValidator.IsValid("123"));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_ForLetters()
    {
        Assert.False(CpfValidator.IsValid("abc.def.ghi-jk"));
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("123.456.789-09", "12345678909")]
    [InlineData("111.444.777-35", "11144477735")]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Strip_ShouldReturnOnlyDigits(string? input, string expected)
    {
        Assert.Equal(expected, CpfValidator.Strip(input));
    }
}

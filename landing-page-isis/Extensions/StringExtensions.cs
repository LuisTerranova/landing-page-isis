namespace landing_page_isis.Extensions;

public static class StringExtensions
{
    public static string FormatPhone(this string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return phone;

        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 11)
            return $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}";

        if (digits.Length == 10)
            return $"({digits[..2]}) {digits.Substring(2, 4)}-{digits[6..]}";

        return phone;
    }

    public static string FormatCpf(this string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return "";

        var digits = new string(cpf.Where(char.IsDigit).ToArray());

        if (digits.Length == 11)
            return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}";

        return cpf ?? "";
    }
}

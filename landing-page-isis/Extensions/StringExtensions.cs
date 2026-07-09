namespace landing_page_isis.Extensions;

/// <summary>
/// Provides string formatting extension methods, specifically for phone numbers and Brazilian CPFs.
/// </summary>
public static class StringExtensions
{
    public static string FormatPhone(this string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return phone;

        // Keep only digits to normalize the raw input string
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // Mobile format: 11 digits (e.g., (69) 99999-9999)
        if (digits.Length == 11)
            return $"({digits[..2]}) {digits.Substring(2, 5)}-{digits[7..]}";

        // Landline format: 10 digits (e.g., (69) 3333-3333)
        if (digits.Length == 10)
            return $"({digits[..2]}) {digits.Substring(2, 4)}-{digits[6..]}";

        return phone;
    }

    public static string FormatCpf(this string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return "";

        // Keep only digits to normalize the raw input string
        var digits = new string(cpf.Where(char.IsDigit).ToArray());

        // Valid CPFs must contain exactly 11 digits
        if (digits.Length == 11)
            return $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}";

        return cpf ?? "";
    }
}

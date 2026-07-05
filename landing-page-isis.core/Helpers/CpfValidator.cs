namespace landing_page_isis.core.Helpers;

public static class CpfValidator
{
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        var digits = new string(cpf.Where(char.IsDigit).ToArray());

        if (digits.Length != 11)
            return false;

        if (digits.All(c => c == digits[0]))
            return false;

        var numbers = digits.Select(c => c - '0').ToArray();

        var sum1 = 0;
        for (var i = 0; i < 9; i++)
            sum1 += numbers[i] * (10 - i);

        var digit1 = sum1 % 11;
        digit1 = digit1 < 2 ? 0 : 11 - digit1;

        if (numbers[9] != digit1)
            return false;

        var sum2 = 0;
        for (var i = 0; i < 10; i++)
            sum2 += numbers[i] * (11 - i);

        var digit2 = sum2 % 11;
        digit2 = digit2 < 2 ? 0 : 11 - digit2;

        return numbers[10] == digit2;
    }

    public static string Strip(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        return new string(cpf.Where(char.IsDigit).ToArray());
    }
}

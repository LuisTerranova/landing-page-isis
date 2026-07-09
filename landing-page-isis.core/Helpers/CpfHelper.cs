using System.Security.Cryptography;
using System.Text;

namespace landing_page_isis.core.Helpers;

public static class CpfHelper
{
    public static string ComputeHash(string strippedCpf)
    {
        var pepper =
            Environment.GetEnvironmentVariable("CPF_HASH_PEPPER")
            ?? throw new InvalidOperationException(
                "CPF_HASH_PEPPER environment variable is not set"
            );
        var data = Encoding.UTF8.GetBytes(strippedCpf + ":" + pepper);
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), data);
        return Convert.ToHexStringLower(hash);
    }
}

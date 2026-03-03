using System.Security.Cryptography;
using System.Text;

namespace landing_page_isis.Services;

public static class AesEncryptionService
{
    private static byte[] GetKey()
    {
        // 32 bytes for AES-256
        var secret = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");

        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("ENCRYPTION_KEY environment variable is not set");

        var key = Encoding.UTF8.GetBytes(secret);

        switch (key.Length)
        {
            case < 32:
            {
                var padded = new byte[32];
                Array.Copy(key, padded, Math.Min(key.Length, 32));
                return padded;
            }
            case > 32:
                return key[..32];
            default:
                return key;
        }
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aesAlg = Aes.Create();
        aesAlg.Key = GetKey();
        aesAlg.GenerateIV();

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        // Prepend IV to the stream
        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aesAlg = Aes.Create();
            aesAlg.Key = GetKey();

            var iv = new byte[aesAlg.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aesAlg.IV = iv;

            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }
}

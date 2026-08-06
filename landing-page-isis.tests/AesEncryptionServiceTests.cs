using System.Security.Cryptography;
using landing_page_isis.Infrastructure;
using Xunit;

namespace landing_page_isis.tests;

public class AesEncryptionServiceTests : IDisposable
{
    private readonly string? _originalKey;
    private const string DummyTestKey = "UnitTestDummyKey00000000000000000";

    public AesEncryptionServiceTests()
    {
        // Save original key from environment if present
        _originalKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
        // Always use a non-secret dummy key for unit testing in isolation
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", DummyTestKey);
    }

    public void Dispose()
    {
        // Restore original environment state to avoid side effects
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", _originalKey);
    }

    [Theory]
    [InlineData("123.456.789-00")]
    [InlineData("paciente@exemplo.com")]
    [InlineData("(11) 99999-8888")]
    [InlineData("Texto longo com caracteres especiais: ç, á, é, ã, 🚀")]
    public void Encrypt_And_Decrypt_ShouldReturnOriginalString(string originalText)
    {
        var encrypted = AesEncryptionService.Encrypt(originalText);

        Assert.NotNull(encrypted);
        Assert.NotEqual(originalText, encrypted);

        var decrypted = AesEncryptionService.Decrypt(encrypted);

        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void Encrypt_NullOrEmpty_ShouldReturnSameValue()
    {
        Assert.Null(AesEncryptionService.Encrypt(null!));
        Assert.Equal(string.Empty, AesEncryptionService.Encrypt(string.Empty));
    }

    [Fact]
    public void Decrypt_NullOrEmpty_ShouldReturnSameValue()
    {
        Assert.Null(AesEncryptionService.Decrypt(null!));
        Assert.Equal(string.Empty, AesEncryptionService.Decrypt(string.Empty));
    }

    [Fact]
    public void Decrypt_InvalidBase64OrCorruptedData_ShouldThrowCryptographicException()
    {
        Assert.Throws<CryptographicException>(() => AesEncryptionService.Decrypt("InvalidBase64CipherData!!!"));
    }
}

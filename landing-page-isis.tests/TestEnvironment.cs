using System.Runtime.CompilerServices;

namespace landing_page_isis.tests;

/// <summary>
/// Sets deterministic environment defaults for the test assembly before any test runs.
/// </summary>
internal static class TestEnvironment
{
    internal const string EncryptionKey = "test-encryption-key-32-bytes----";
    internal const string CpfHashPepper = "test-pepper-secret";

    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", EncryptionKey);
        Environment.SetEnvironmentVariable("CPF_HASH_PEPPER", CpfHashPepper);
    }
}

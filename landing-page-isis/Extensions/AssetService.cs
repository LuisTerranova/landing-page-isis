namespace landing_page_isis.Extensions;

public static class AssetService
{
    private static byte[]? _separatorBytes;
    private static string? _separatorDataUri;

    private static string ResolvePath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "isis-content-separator.png"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "isis-content-separator.png"),
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "isis-content-separator.png not found. Checked: " + string.Join(", ", candidates)
            );
    }

    public static byte[] GetSeparatorBytes()
    {
        if (_separatorBytes != null)
            return _separatorBytes;

        _separatorBytes = File.ReadAllBytes(ResolvePath());
        return _separatorBytes;
    }

    public static string GetSeparatorDataUri()
    {
        if (_separatorDataUri != null)
            return _separatorDataUri;

        var bytes = GetSeparatorBytes();
        var base64 = Convert.ToBase64String(bytes);
        _separatorDataUri = $"data:image/png;base64,{base64}";
        return _separatorDataUri;
    }
}

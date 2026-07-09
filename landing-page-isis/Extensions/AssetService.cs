namespace landing_page_isis.Extensions;

public static class AssetService
{
    private static byte[]? _separatorBytes;
    private static string? _separatorDataUri;

    private static string ResolvePath()
    {
        var candidates = new List<string>();

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            candidates.Add(Path.Combine(current.FullName, "wwwroot", "isis-content-separator.png"));

            foreach (var sub in current.EnumerateDirectories())
            {
                candidates.Add(Path.Combine(sub.FullName, "wwwroot", "isis-content-separator.png"));
            }

            current = current.Parent;
        }

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "isis-content-separator.png not found."
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

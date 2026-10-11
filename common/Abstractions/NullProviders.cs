namespace retro;

public sealed class NullLogger : ILogger
{
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message) { }
}

public sealed class NullAssetProvider : IAssetProvider
{
    public bool Exists(string relativePath) => false;
    public byte[]? ReadAllBytes(string relativePath) => null;
    public IEnumerable<string> ListFiles(string relativeSubdir) => Array.Empty<string>();
}

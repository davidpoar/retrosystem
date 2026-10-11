namespace retro;

public interface IAssetProvider
{
    bool Exists(string relativePath);
    byte[]? ReadAllBytes(string relativePath);
    IEnumerable<string> ListFiles(string relativeSubdir);
}

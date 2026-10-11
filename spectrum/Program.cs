using System;
using System.IO;
using retro;
using retro.spectrum;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        string? tzxPath = null;
        bool fastLoad = false;
        string? romName = null;

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-t" || args[i] == "--tape") && i + 1 < args.Length)
            {
                tzxPath = args[i + 1];
                i++;
            }
            else if (args[i] == "-f" || args[i] == "--fastload")
            {
                fastLoad = true;
            }
            else if ((args[i] == "-r" || args[i] == "--rom") && i + 1 < args.Length)
            {
                romName = args[i + 1];
                i++;
            }
        }

        Console.WriteLine("Iniciando emulador ZX Spectrum 48K...");
        using var game = new SpectrumGame(tzxPath, fastLoad, romName,
            new ConsoleLogger(), new FileSystemAssetProvider());
        game.Run();
        return 0;
    }
}

internal sealed class ConsoleLogger : ILogger
{
    public void Info(string message) => Console.WriteLine(message);
    public void Warning(string message) => Console.WriteLine("[WARN] " + message);
    public void Error(string message) => Console.Error.WriteLine("[ERROR] " + message);
}

internal sealed class FileSystemAssetProvider : IAssetProvider
{
    private readonly string[] _searchPaths;
    public FileSystemAssetProvider()
    {
        _searchPaths = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "spectrum"),
            AppDomain.CurrentDomain.BaseDirectory,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum")
        }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    public bool Exists(string relativePath)
    {
        foreach (var basePath in _searchPaths)
        {
            var candidate = Path.Combine(basePath, relativePath);
            if (File.Exists(candidate)) return true;
            if (Path.IsPathRooted(relativePath) && File.Exists(relativePath)) return true;
        }
        return false;
    }
    public byte[]? ReadAllBytes(string relativePath)
    {
        if (Path.IsPathRooted(relativePath) && File.Exists(relativePath))
            return File.ReadAllBytes(relativePath);
        foreach (var basePath in _searchPaths)
        {
            var candidate = Path.Combine(basePath, relativePath);
            if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
        }
        return null;
    }
    public IEnumerable<string> ListFiles(string relativeSubdir)
    {
        foreach (var basePath in _searchPaths)
        {
            var dir = string.IsNullOrEmpty(relativeSubdir) || relativeSubdir == "."
                ? basePath
                : Path.Combine(basePath, relativeSubdir);
            if (Directory.Exists(dir))
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                    yield return file;
                yield break;
            }
        }
    }
}

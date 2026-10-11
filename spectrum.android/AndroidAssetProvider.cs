using System;
using System.Collections.Generic;
using System.IO;
using Android.Content;

namespace retro.spectrum.android;

public sealed class AndroidAssetProvider : IAssetProvider
{
    private const string AssetRoot = "spectrum";
    private readonly Context _context;
    private readonly string _externalCacheDir = "";

    public AndroidAssetProvider(Context context)
    {
        _context = context;
        var ext = context.ExternalCacheDir?.AbsolutePath;
        _externalCacheDir = !string.IsNullOrEmpty(ext) ? ext! : context.CacheDir!.AbsolutePath!;
    }

    public bool Exists(string relativePath)
    {
        var assetPath = $"{AssetRoot}/{relativePath}";
        if (TryReadAsset(assetPath) != null) return true;
        var external = Path.Combine(_externalCacheDir, relativePath);
        return File.Exists(external);
    }

    public byte[]? ReadAllBytes(string relativePath)
    {
        var assetPath = $"{AssetRoot}/{relativePath}";
        var fromAsset = TryReadAsset(assetPath);
        if (fromAsset != null) return fromAsset;
        var external = Path.Combine(_externalCacheDir, relativePath);
        if (File.Exists(external)) return File.ReadAllBytes(external);
        return null;
    }

    public IEnumerable<string> ListFiles(string relativeSubdir)
    {
        foreach (var name in ListAssetNames(relativeSubdir))
        {
            yield return $"{AssetRoot}/{name}";
        }

        var external = Path.Combine(_externalCacheDir, relativeSubdir);
        if (!string.IsNullOrEmpty(relativeSubdir) && relativeSubdir != "." && Directory.Exists(external))
        {
            foreach (var f in Directory.EnumerateFiles(external))
                yield return f;
        }
    }

    private byte[]? TryReadAsset(string assetPath)
    {
        var ctx = _context;
        if (ctx == null) return null;
        try
        {
            using var stream = ctx.Assets!.Open(assetPath);
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private string[] ListAssetNames(string relativeSubdir)
    {
        var ctx = _context;
        if (ctx == null) return Array.Empty<string>();
        var assetPath = string.IsNullOrEmpty(relativeSubdir) || relativeSubdir == "."
            ? AssetRoot
            : $"{AssetRoot}/{relativeSubdir}";
        try
        {
            return ctx.Assets!.List(assetPath) ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}

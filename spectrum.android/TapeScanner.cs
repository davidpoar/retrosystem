using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Content;

namespace retro.spectrum.android;

public static class TapeScanner
{
    public static List<TapeInfo> ScanAll(Context context, int maxResults = 200)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<TapeInfo>();

        var externalDirs = new List<string>();

        try
        {
            var ext = context.ExternalCacheDir?.AbsolutePath;
            if (!string.IsNullOrEmpty(ext)) externalDirs.Add(ext);
            var cache = context.CacheDir?.AbsolutePath;
            if (!string.IsNullOrEmpty(cache)) externalDirs.Add(cache);
            var files = context.GetExternalFilesDir(null)?.AbsolutePath;
            if (!string.IsNullOrEmpty(files)) externalDirs.Add(files);
        }
        catch { }

        try
        {
            using var env = Android.OS.Environment.ExternalStorageDirectory;
            if (env != null)
            {
                var path = env.AbsolutePath;
                externalDirs.Add(path);
                externalDirs.Add(Path.Combine(path, "Download"));
                externalDirs.Add(Path.Combine(path, "Downloads"));
                externalDirs.Add(Path.Combine(path, "Spectrum"));
                externalDirs.Add(Path.Combine(path, "Tapes"));
                externalDirs.Add(Path.Combine(path, "ZXSpectrum"));
            }
        }
        catch { }

        foreach (var dir in externalDirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            ScanDirectory(dir, seen, results, maxResults);
            if (results.Count >= maxResults) break;
        }

        return results.OrderByDescending(t => t.LastModified).Take(maxResults).ToList();
    }

    private static void ScanDirectory(string dir, HashSet<string> seen, List<TapeInfo> results, int max)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.tap", SearchOption.TopDirectoryOnly))
                AddIfNew(file, seen, results);
            foreach (var file in Directory.EnumerateFiles(dir, "*.tzx", SearchOption.TopDirectoryOnly))
                AddIfNew(file, seen, results);
        }
        catch { }
    }

    private static void AddIfNew(string path, HashSet<string> seen, List<TapeInfo> results)
    {
        if (results.Count >= 200) return;
        if (seen.Contains(path)) return;
        try
        {
            var info = TapeInfoFactory.FromFile(path);
            if (info.SizeBytes <= 0) return;
            seen.Add(path);
            results.Add(info);
        }
        catch { }
    }
}

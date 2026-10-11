using System;
using System.IO;

namespace retro.spectrum.android;

public sealed class TapeInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public TapeFormat Format { get; set; }
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsRecent { get; set; }
    public DateTime? LastPlayed { get; set; }

    public string FormatTag => Format == TapeFormat.TAP ? "TAP" : "TZX";

    public string FormatColor => Format == TapeFormat.TAP ? "tap" : "tzx";

    public string Meta
    {
        get
        {
            var sizeText = SizeBytes switch
            {
                < 1024 => $"{SizeBytes} B",
                < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
                _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB"
            };
            return $"{sizeText} · {LastModified:yyyy-MM-dd}";
        }
    }
}

public enum TapeFormat
{
    TAP,
    TZX
}

public static class TapeInfoFactory
{
    public static TapeInfo FromFile(string fullPath, bool isRecent = false, DateTime? lastPlayed = null)
    {
        var fileName = Path.GetFileName(fullPath);
        var displayName = Path.GetFileNameWithoutExtension(fullPath);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var format = ext switch
        {
            ".tap" => TapeFormat.TAP,
            ".tzx" => TapeFormat.TZX,
            _ => TapeFormat.TAP
        };

        var fi = new FileInfo(fullPath);
        return new TapeInfo
        {
            FilePath = fullPath,
            FileName = fileName,
            DisplayName = displayName,
            Format = format,
            SizeBytes = fi.Exists ? fi.Length : 0,
            LastModified = fi.Exists ? fi.LastWriteTime : DateTime.MinValue,
            IsRecent = isRecent,
            LastPlayed = lastPlayed
        };
    }
}

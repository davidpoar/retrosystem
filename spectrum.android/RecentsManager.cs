using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;

namespace retro.spectrum.android;

public sealed class RecentsManager
{
    private const string PrefsName = "spectrum_recents";
    private const string KeyRecents = "recents";
    private const int MaxRecents = 8;

    private readonly Context _context;
    private readonly ISharedPreferences _prefs;

    public RecentsManager(Context context)
    {
        _context = context;
        _prefs = _context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
    }

    public List<RecentEntry> GetAll()
    {
        var raw = _prefs.GetString(KeyRecents, "");
        if (string.IsNullOrEmpty(raw)) return new List<RecentEntry>();
        var list = new List<RecentEntry>();
        foreach (var line in raw.Split('|'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(';');
            if (parts.Length < 2) continue;
            list.Add(new RecentEntry
            {
                Path = parts[0],
                DisplayName = parts[1],
                LastPlayedUtc = long.TryParse(parts[2], out var ts) ? ts : 0
            });
        }
        return list.OrderByDescending(r => r.LastPlayedUtc).ToList();
    }

    public void Add(string filePath, string displayName)
    {
        var list = GetAll();
        list.RemoveAll(r => string.Equals(r.Path, filePath, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, new RecentEntry
        {
            Path = filePath,
            DisplayName = displayName,
            LastPlayedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        if (list.Count > MaxRecents) list = list.Take(MaxRecents).ToList();

        var serialized = string.Join("|", list.Select(r =>
            $"{r.Path};{r.DisplayName};{r.LastPlayedUtc}"));
        _prefs.Edit().PutString(KeyRecents, serialized).Apply();
    }

    public void Clear()
    {
        _prefs.Edit().Remove(KeyRecents).Apply();
    }

    public bool IsRecent(string filePath)
    {
        return GetAll().Any(r => string.Equals(r.Path, filePath, StringComparison.OrdinalIgnoreCase));
    }

    public DateTime? GetLastPlayed(string filePath)
    {
        var entry = GetAll().FirstOrDefault(r => string.Equals(r.Path, filePath, StringComparison.OrdinalIgnoreCase));
        if (entry == null) return null;
        return DateTimeOffset.FromUnixTimeSeconds(entry.LastPlayedUtc).LocalDateTime;
    }
}

public sealed class RecentEntry
{
    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public long LastPlayedUtc { get; set; }
}

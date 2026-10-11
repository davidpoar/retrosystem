using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace retro.spectrum.android;

public sealed class ZxInfoClient
{
    public const string BaseUrl = "https://api.zxinfo.dk/v3";
    public const string StaticBaseUrl = "https://worldofspectrum.net";

    private readonly HttpClient _http;

    public ZxInfoClient()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Add("User-Agent", "RetroSystem-Spectrum/1.3");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<ZxInfoSearchResponse> SearchAsync(string query, int from = 0, int size = 25, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&from={from}&size={size}";
        try
        {
            var json = await _http.GetStringAsync(url, ct);
            var response = JsonSerializer.Deserialize<ZxInfoSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return response ?? new ZxInfoSearchResponse();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("ZxInfoClient", $"Search fallo: {ex.Message}");
            throw;
        }
    }

    public string MakeAbsoluteUrl(string relative)
    {
        if (string.IsNullOrEmpty(relative)) return "";
        if (relative.StartsWith("http://") || relative.StartsWith("https://")) return relative;
        if (relative.StartsWith("/")) return StaticBaseUrl + relative;
        return StaticBaseUrl + "/" + relative;
    }

    public async Task<byte[]> DownloadFileAsync(string absoluteUrl, IProgress<long>? progress = null, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(absoluteUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var ms = new MemoryStream();

        var buffer = new byte[8192];
        long read = 0;
        int n;
        while ((n = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            ms.Write(buffer, 0, n);
            read += n;
            progress?.Report(read);
        }
        return ms.ToArray();
    }

    public ZxInfoFile? FindBestDownload(ZxInfoSource source)
    {
        var available = source.Availability?.ToLowerInvariant() ?? "";
        if (available.Contains("denied") || available.Contains("never released") || available.Contains("mia"))
        {
            return null;
        }

        var allFiles = source.Releases.SelectMany(r => r.Files).ToList();
        var candidates = new List<ZxInfoFile>();
        candidates.AddRange(allFiles.Where(f => f.Path.EndsWith(".tzx.zip", StringComparison.OrdinalIgnoreCase)
                                              || f.Path.EndsWith(".tzx", StringComparison.OrdinalIgnoreCase)
                                              || f.Path.EndsWith(".tap.zip", StringComparison.OrdinalIgnoreCase)
                                              || f.Path.EndsWith(".tap", StringComparison.OrdinalIgnoreCase)));
        candidates.AddRange(allFiles.Where(f => f.Path.EndsWith(".dsk.zip", StringComparison.OrdinalIgnoreCase)
                                              || f.Path.EndsWith(".dsk", StringComparison.OrdinalIgnoreCase)));

        foreach (var f in candidates)
        {
            if (f.Path.StartsWith("/pub/"))
            {
                return f;
            }
        }
        return candidates.FirstOrDefault();
    }
}

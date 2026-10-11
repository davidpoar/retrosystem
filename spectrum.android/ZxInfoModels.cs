using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace retro.spectrum.android;

public sealed class ZxInfoSearchResponse
{
    [JsonPropertyName("took")]
    public int Took { get; set; }

    [JsonPropertyName("hits")]
    public ZxInfoHits? Hits { get; set; }
}

public sealed class ZxInfoHits
{
    [JsonPropertyName("total")]
    public ZxInfoTotal? Total { get; set; }

    [JsonPropertyName("hits")]
    public List<ZxInfoHit> Hits { get; set; } = new();
}

public sealed class ZxInfoTotal
{
    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("relation")]
    public string Relation { get; set; } = "eq";
}

public sealed class ZxInfoHit
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("_source")]
    public ZxInfoSource? Source { get; set; }
}

public sealed class ZxInfoSource
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("originalYearOfRelease")]
    public int? Year { get; set; }

    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    [JsonPropertyName("machineType")]
    public string? MachineType { get; set; }

    [JsonPropertyName("availability")]
    public string? Availability { get; set; }

    [JsonPropertyName("publishers")]
    public List<ZxInfoLabel> Publishers { get; set; } = new();

    [JsonPropertyName("authors")]
    public List<ZxInfoAuthor> Authors { get; set; } = new();

    [JsonPropertyName("releases")]
    public List<ZxInfoRelease> Releases { get; set; } = new();

    [JsonPropertyName("screens")]
    public List<ZxInfoScreen> Screens { get; set; } = new();
}

public sealed class ZxInfoLabel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public sealed class ZxInfoAuthor
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public sealed class ZxInfoRelease
{
    [JsonPropertyName("files")]
    public List<ZxInfoFile> Files { get; set; } = new();
}

public sealed class ZxInfoFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class ZxInfoScreen
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

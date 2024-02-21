using System;
using System.Text.Json.Serialization;

namespace ImmichFrame.Models;

public class AssetInfo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("fileCreatedAt")]
    public DateTime FileCreatedAt { get; set; }

    [JsonIgnore]
    public string ImageUrl => $"/api/asset/thumbnail/{Id}?format=JPEG";
}


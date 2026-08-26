using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Leaves_KeyPrefixer;

public record ModConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("prefixName")]
    public bool PrefixName { get; set; } = true;

    [JsonPropertyName("prefixShortName")]
    public bool PrefixShortName { get; set; } = true;

    [JsonPropertyName("mapPrefixes")]
    public Dictionary<string, string> MapPrefixes { get; set; } = new Dictionary<string, string>();
}

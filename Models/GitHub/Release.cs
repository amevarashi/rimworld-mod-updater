using System.Text.Json.Serialization;

namespace RimWorldModUpdater.Models.GitHub;

public class Release
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }
    
    [JsonPropertyName("assets")]
    public ReleaseAsset[]? Assets { get; set; }
}
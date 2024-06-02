using System.Text.Json.Serialization;

namespace RimWorldModUpdater.Models.GitHub;

public class ReleaseAsset
{
    [JsonPropertyName("name")]
    public string? Name { get; set;}

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? DownloadUrl { get; set; }
}
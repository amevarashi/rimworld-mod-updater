using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldModUpdater.Models;

public static class SelfUpdateManager
{
    private const string URI = "https://api.github.com/repos/amevarashi/rimworld-mod-updater/releases/latest";

    public static async Task<string?> CheckUpdates(CancellationToken cancellationToken = default)
    {
        using HttpClient httpClient = CreateClient();
        using HttpResponseMessage response = await httpClient.GetAsync(URI, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            App.Log.Information($"Failed to check updates - responded {response.StatusCode}");
            return null;
        }

        try
        {
            GitHub.Release? release = await response.Content.ReadFromJsonAsync<GitHub.Release>(cancellationToken);

            if (release is null)
            {
                App.Log.Information("Failed to check updates - empty release json");
                return null;
            }

            if (release.TagName is null)
            {
                App.Log.Information("Failed to check updates - latest release has no tag name");
                return null;
            }

            if (release.Assets is null || release.Assets.Length == 0)
            {
                App.Log.Information("Failed to check updates - empty Assets");
                return null;
            }

            Version latestVer = new(release.TagName);

            if (latestVer <= App.Version)
            {
                App.Log.Information("Checked updates - on the latest version");
                return null;
            }

            GitHub.ReleaseAsset? asset = null;

            if (OperatingSystem.IsWindows())
            {
                App.Log.Information("Platform is Windows");
                asset = release.Assets.FirstOrDefault(x => x.ContentType == "application/x-ms-dos-executable");
            }
            else if (OperatingSystem.IsLinux())
            {
                App.Log.Information("Platform is Linux");
                asset = release.Assets.FirstOrDefault(x => x.ContentType == "application/octet-stream");
            }
            else
            {
                App.Log.Information("Failed to check updates - unknown OS");
                return null;
            }

            if (asset is null)
            {
                App.Log.Information("Failed to check updates - asset not found");
                return null;
            }

            App.Log.Information($"Found new version {asset.Name} {asset.DownloadUrl}");
            return asset.DownloadUrl;
        }
        catch (Exception e)
        {
            App.Log.Information(e, "Failed to check updates");
            return null;
        }
    }

    public static async Task DownloadAsync(string downloadUri, string saveAs, IProgress<float> progress, CancellationToken cancellationToken)
    {
        using MemoryStream content = new();
        using HttpClient httpClient = CreateClient();
        await httpClient.DownloadAsync(downloadUri, content, progress, cancellationToken);

        UnixFileMode unixPerms = default;

        if (OperatingSystem.IsLinux())
        {
            unixPerms = File.GetUnixFileMode(saveAs);

            if (!unixPerms.HasFlag(UnixFileMode.UserExecute))
            {
                // Process.Start file without this flag locks up current process. Cute
                unixPerms |= UnixFileMode.UserExecute;
            }
        }

        FileInfo currentBundle = new(saveAs);
        DirectoryInfo currentDir = currentBundle.Directory!;
        FileInfo oldBundle = new(Path.Combine(currentDir.FullName, currentBundle.Name + ".bak"));

        if (oldBundle.Exists)
        {
            App.Log.Information($"Deleting {oldBundle.Name}");
            oldBundle.Delete();
        }

        App.Log.Information($"Renaming {currentBundle.Name} to {oldBundle.Name}");
        currentBundle.MoveTo(oldBundle.FullName);

        currentBundle = new(saveAs);
        App.Log.Information($"Saving downloaded file as {currentBundle.Name}");
        using FileStream fileStream = currentBundle.OpenWrite();
        content.Position = 0;
        content.CopyTo(fileStream);
        content.Close();
        fileStream.Close();

        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(currentBundle.FullName, unixPerms);
        }
    }

    private static HttpClient CreateClient()
    {
        HttpClient httpClient = new();
        ProductHeaderValue header = new("rimworld-mod-updater", App.Version.ToString());
        httpClient.DefaultRequestHeaders.UserAgent.Add(new(header));
        return httpClient;
    }
}
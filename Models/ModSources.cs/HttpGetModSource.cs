using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Reloaded.Memory.Extensions;

namespace RimWorldModUpdater.Models.ModSources;

public class HttpGetModSource(ModSourceInfo modSourceInfo) : IModSource
{
    public readonly string AboutLink = modSourceInfo.HttpGetAboutLink;
    public readonly string ManifestLink = modSourceInfo.HttpGetManifestLink;
    public readonly string DownloadLink = modSourceInfo.HttpGetDownloadLink;

    private readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromMinutes(30) };

    public async Task<string> FetchVersionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ManifestLink))
        {
            return string.Empty;
        }

        using HttpResponseMessage response = await httpClient.GetAsync(ManifestLink, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            App.Log.Warning($"{ManifestLink} - responded {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return string.Empty;
            }
            return $"HTTP {response.StatusCode}";
        }

        if (response.Content.Headers.ContentType?.MediaType != "text/plain" &&
            response.Content.Headers.ContentType?.MediaType != "text/xml")
        {
            App.Log.Error($"{ManifestLink} - MediaType was {response.Content.Headers.ContentType?.MediaType}");
            return "Unavailable";
        }

        string xml = await response.Content.ReadAsStringAsync(cancellationToken);
        RimWorldModManifest manifest = RimWorldModManifest.FromXml(xml);
        return manifest.Version;
    }

    public async Task<RimWorldModAbout?> FetchAboutAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(AboutLink, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            App.Log.Warning($"{AboutLink} - responded {response.StatusCode}");
            return null;
        }

        if (response.Content.Headers.ContentType?.MediaType != "text/plain" &&
            response.Content.Headers.ContentType?.MediaType != "text/xml")
        {
            App.Log.Error($"{AboutLink} - MediaType was {response.Content.Headers.ContentType?.MediaType}");
            return null;
        }

        string xml = await response.Content.ReadAsStringAsync(cancellationToken);
        return RimWorldModAbout.FromXml(xml);
    }

    public async Task DownloadAsync(RimWorldMod mod, IProgress<float> progress, CancellationToken cancellationToken)
    {
        MemoryStream content = new();
        await httpClient.DownloadAsync(DownloadLink, content, progress, cancellationToken);
        using ZipArchive zip = new(content);
        string modsPath = App.UserSettings!.RimWorldModFolder;

        // found a mod that had About.xml in Languages
        ZipArchiveEntry? aboutInZip = zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("About/About.xml"));
        aboutInZip ??= zip.Entries.FirstOrDefault(x => x.FullName.EndsWith("About\\About.xml"));

        if (aboutInZip is null)
        {
            App.Log.Error($"Mod {mod.Name} on remote does not have About.xml");
            return; // Stop update
        }

        int deepth = aboutInZip.FullName.Count('/');

        if (deepth == 0)
        {
            deepth = aboutInZip.FullName.Count('\'');
        }

        bool zipWithRootFolder = deepth == 2;

        if (mod.LocalDir is null)
        {
            mod.LocalDir ??= new(Path.Combine(modsPath, mod.Name));
        }
        else
        {
            mod.LocalDir.Refresh();
        }

        if (mod.LocalDir.Exists)
        {
            mod.LocalDir.Delete(true);
        }

        if (zipWithRootFolder)
        {
            zip.ExtractToDirectory(modsPath);
            string newFolderName = zip.Entries[0].FullName.Split('/', '\'')[0];
            mod.LocalDir = new(Path.Combine(modsPath, newFolderName));
        }
        else
        {
            mod.LocalDir!.Create();
            zip.ExtractToDirectory(mod.LocalDir!.FullName);
        }
        content.Dispose();
        mod.LocalVersion = mod.RemoteVersion;
    }

    public override string ToString()
    {
        return $"HttpGetModSource[{DownloadLink}]";
    }
}
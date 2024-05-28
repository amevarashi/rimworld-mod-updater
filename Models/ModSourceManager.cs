using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RimWorldModUpdater.Models.ModSources;

namespace RimWorldModUpdater.Models;

public class ModSourceManager
{
    private const string CACHE_FILE_NAME = "modSourcesCache.json";
    private const string URI = "https://gitgud.io/amevarashi/rjw-mod-sources/-/raw/master/ModSources.json";
    private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<IModSource[]> GetModSourcesAsync(CancellationToken cancellationToken)
    {
        string json = await GetModSourceJsonAsync(cancellationToken);

        ModSourceInfo[]? modSourceInfos;

        try
        {
            modSourceInfos = JsonSerializer.Deserialize<ModSourceInfo[]?>(json);
        }
        catch (Exception e)
        {
            App.Log.Fatal(e, "Failed to deserialize modSourceInfos");
            throw;
        }

        if (modSourceInfos is null)
        {
            throw new ModSourcesException("Failed to deserialize modSourceInfos, reason unknown");
        }

        IModSource[] modSources = new IModSource[modSourceInfos.Length];
        string serverRepo;

        for (int i = 0; i < modSourceInfos.Length; i++)
        {
            ModSourceInfo modSourceInfo = modSourceInfos[i];

            switch (modSourceInfo.SourceType)
            {
                case "HttpGet":
                    modSources[i] = new HttpGetModSource(modSourceInfo);
                    break;

                case "GitLab":
                    string repoName = modSourceInfo.Repository.Split('/')[1];
                    serverRepo = $"https://{modSourceInfo.GitLabServer}/{modSourceInfo.Repository}/-";
                    modSourceInfo.HttpGetDownloadLink = $"{serverRepo}/archive/{modSourceInfo.Branch}/{repoName}-{modSourceInfo.Branch}.zip";
                    modSourceInfo.HttpGetAboutLink = $"{serverRepo}/raw/{modSourceInfo.Branch}/About/About.xml";
                    modSourceInfo.HttpGetManifestLink = $"{serverRepo}/raw/{modSourceInfo.Branch}/About/Manifest.xml";
                    modSources[i] = new HttpGetModSource(modSourceInfo);
                    break;

                case "GitGud":
                    repoName = modSourceInfo.Repository.Split('/')[1];
                    serverRepo = $"https://gitgud.io/{modSourceInfo.Repository}/-";
                    modSourceInfo.HttpGetDownloadLink = $"{serverRepo}/archive/{modSourceInfo.Branch}/{repoName}-{modSourceInfo.Branch}.zip";
                    modSourceInfo.HttpGetAboutLink = $"{serverRepo}/raw/{modSourceInfo.Branch}/About/About.xml";
                    modSourceInfo.HttpGetManifestLink = $"{serverRepo}/raw/{modSourceInfo.Branch}/About/Manifest.xml";
                    modSources[i] = new HttpGetModSource(modSourceInfo);
                    break;

                default:
                    App.Log.Error($"Unknown mod SourceType \"{modSourceInfo.SourceType}\"");
                    break;
            }
        }

        return modSources;
    }

    private static async Task<string> GetModSourceJsonAsync(CancellationToken cancellationToken)
    {
        string? json = await LoadCacheAsync(App.SettingFolder, cancellationToken);

        if (json != null)
        {
            App.Log.Debug("ModSources loaded from cache");
            return json;
        }

        json = await DownloadModSourceInfosAsync(URI, cancellationToken);

        if (json is null)
        {
            throw new ModSourcesException($"Failed to download modSourceInfos from {URI}, reason unknown");
        }

        await SaveCacheAsync(App.SettingFolder, json, cancellationToken);

        return json;
    }

    private static async Task<string> DownloadModSourceInfosAsync(string uri, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(uri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ModSourcesException($"{uri} - responded {response.StatusCode}");
        }

        if (response.Content.Headers.ContentType?.MediaType != "text/plain")
        {
            throw new ModSourcesException($"{uri} - MediaType was {response.Content.Headers.ContentType?.MediaType}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static async Task<string?> LoadCacheAsync(string directoryPath, CancellationToken cancellationToken)
    {
        FileInfo cache = new(Path.Combine(directoryPath, CACHE_FILE_NAME));

        if (!cache.Exists)
        {
            return null;
        }

        if (cache.LastWriteTimeUtc.AddHours(1) < DateTime.UtcNow)
        {
            App.Log.Debug("modSourcesCache is stale");
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(Path.Combine(directoryPath, CACHE_FILE_NAME), cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task SaveCacheAsync(string directoryPath, string json, CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directoryPath, CACHE_FILE_NAME), json, cancellationToken);
        }
        catch (Exception e)
        {
            App.Log.Error(e, "Failed to save modSourceInfos cache");
        }
    }
}
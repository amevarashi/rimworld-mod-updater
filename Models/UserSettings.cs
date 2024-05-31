using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using NexusMods.Paths;

namespace RimWorldModUpdater.Models;

public class UserSettings
{
    public const string FILE_NAME = "settings.json";
    public string rimWorldFolder = string.Empty;
    public string RimWorldFolder { get => rimWorldFolder; set => rimWorldFolder = value; }
    public string RimWorldModFolder => Path.Combine(RimWorldFolder, "Mods");
    public string ActiveLocale { get; set; } = "en_US";
    public string modSourcesUri = string.Empty;
    public string ModSourcesUri { get => modSourcesUri; set => modSourcesUri = value; }
    private readonly JsonSerializerOptions options = new() { WriteIndented = true };

    public static UserSettings? Load(string path)
    {
        try
        {
            string json = File.ReadAllText(Path.Combine(path, FILE_NAME));
            return JsonSerializer.Deserialize<UserSettings?>(json);
        }
        catch (Exception)
        {
            App.Log.Warning("Failed to load UserSettings");
            return null;
        }
    }

    public static UserSettings Create()
    {
        UserSettings userSettings = new()
        {
            RimWorldFolder = TryFindRimWorldFolder(),
            ModSourcesUri = "https://gitgud.io/amevarashi/rjw-mod-sources/-/raw/master/ModSources.json"
        };

        return userSettings;
    }

    public static string TryFindRimWorldFolder()
    {
        SteamHandler handler = new(FileSystem.Shared, OperatingSystem.IsWindows() ? GameFinder.RegistryUtils.WindowsRegistry.Shared : null);
        SteamGame? game = handler.FindOneGameById(AppId.From(294100), out GameFinder.Common.ErrorMessage[]? errors);

        foreach (GameFinder.Common.ErrorMessage error in errors)
        {
            App.Log.Error(error.ToString());
        }

        if (game != null)
        {
            return game.Path.ToString();
        }

        return string.Empty;
    }

    public async Task SaveAsync(string path)
    {
        try
        {
            string json = JsonSerializer.Serialize(this, options);
            await File.WriteAllTextAsync(Path.Combine(path, FILE_NAME), json);
            App.Log.Debug("Saved UserSettings");
        }
        catch (Exception e)
        {
            App.Log.Error(e, "Failed to save UserSettings");
        }
    }
}
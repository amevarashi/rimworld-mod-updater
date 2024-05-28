using System.Collections.Generic;
using System.IO;
using ReactiveUI;

namespace RimWorldModUpdater.Models;

public class RimWorldMod : ReactiveObject
{
    private static readonly Dictionary<string, RimWorldMod> allMods = [];
    private string localVersion = string.Empty;
    private string remoteVersion = string.Empty;

    public string Id => about.Id;
    public string Name => about.Name;
    public string Author => about.Author;
    public string Description => about.Description;
    public HashSet<string> SupportedVersions => about.SupportedVersions;
    public string LocalVersion
    {
        get => localVersion;
        set => this.RaiseAndSetIfChanged(ref localVersion, value);
    }
    public string RemoteVersion
    {
        get => remoteVersion;
        set => this.RaiseAndSetIfChanged(ref remoteVersion, value);
    }
    public DirectoryInfo? LocalDir { get; set; }
    private readonly RimWorldModAbout about;

    private RimWorldMod(RimWorldModAbout about)
    {
        this.about = about;
    }

    public static RimWorldMod GetSingleton(RimWorldModAbout about)
    {
        if (allMods.TryGetValue(about.Id, out RimWorldMod? mod))
        {
            return mod;
        }

        mod = new(about);
        allMods.Add(about.Id, mod);
        return mod;
    }

    public override string ToString()
    {
        return $"RimWorldMod[{Id}]";
    }
}
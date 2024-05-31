using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using RimWorldModUpdater.Models;

namespace RimWorldModUpdater.ViewModels;

public class ModListViewModel : ViewModelBase
{
    private ObservableCollection<ModViewModel> viewMods = [];
    public ObservableCollection<ModViewModel> ViewMods
    {
        get => viewMods;
        set => this.RaiseAndSetIfChanged(ref viewMods, value);
    }

    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    public ICommand UpdateSelectedCommand { get; }

    public ModListViewModel()
    {
        App.Log.Debug("Creating ModListViewModel");
        UpdateSelectedCommand = ReactiveCommand.CreateFromTask(UpdateSelectedAsync);

        RxApp.MainThreadScheduler.ScheduleAsync((scheduler, cancellationToken) => LoadLocalModsAsync(cancellationToken));
        RxApp.MainThreadScheduler.ScheduleAsync((scheduler, cancellationToken) => RequestRemoteModsAsync(cancellationToken));
    }

    private async Task UpdateSelectedAsync(CancellationToken cancellationToken)
    {
        IEnumerable<ModViewModel> selectedMods = ViewMods.Where(x => x.IsSelected);

        IsBusy = true;

        await Parallel.ForEachAsync(selectedMods, cancellationToken, (mod, token) => mod.UpdateAsync(token));

        App.Log.Information("Updating local mods list");
        await LoadLocalModsAsync(cancellationToken);

        IsBusy = false;
    }

    private static async Task LoadLocalModsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(App.UserSettings?.RimWorldFolder))
        {
            return;
        }

        DirectoryInfo modsDir = new(App.UserSettings.RimWorldModFolder);

        foreach (DirectoryInfo modDir in modsDir.EnumerateDirectories())
        {
            DirectoryInfo? aboutDir = modDir.GetDirectories().FirstOrDefault(d => d.Name == "About");

            if (aboutDir is null)
            {
                App.Log.Warning($"About folder is not found in {modDir.Name}");
                continue;
            }

            FileInfo[] files = aboutDir.GetFiles();

            RimWorldModAbout? about = null;
            RimWorldModManifest? manifest = null;

            for (int i = 0; i < files.Length; i++)
            {
                string xml;

                switch (files[i].Name.ToLower())
                {
                    case "about.xml":
                        xml = await File.ReadAllTextAsync(files[i].FullName, cancellationToken);
                        about = RimWorldModAbout.FromXml(xml);
                        break;
                    case "manifest.xml":
                        xml = await File.ReadAllTextAsync(files[i].FullName, cancellationToken);
                        manifest = RimWorldModManifest.FromXml(xml);
                        break;
                }
            }

            if (about is null)
            {
                App.Log.Warning($"About.xml is not found in {modDir.Name}");
                continue;
            }

            string localVersion = string.Empty;
            if (!string.IsNullOrWhiteSpace(about.ModVersion))
            {
                localVersion = about.ModVersion;
            }
            else if (manifest is not null)
            {
                localVersion = manifest.Version;
            }
            else
            {
                localVersion = "n/a";
            }

            Dictionary<string, RimWorldMod> localMods = [];

            if (!localMods.TryGetValue(about.Id, out RimWorldMod? mod))
            {
                App.Log.Debug($"Adding new local mod {about.Name} {localVersion}");
                mod = RimWorldMod.GetSingleton(about);
                localMods.Add(mod.Id, mod);
            }
            else if (mod.LocalVersion != localVersion && string.IsNullOrWhiteSpace(localVersion))
            {
                App.Log.Debug($"Changing local version of {about.Name}: {mod.LocalVersion} -> {localVersion}");
            }
            mod.LocalVersion = localVersion;
            mod.LocalDir = modDir;
        }
    }

    private async Task RequestRemoteModsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(App.UserSettings?.RimWorldFolder))
        {
            return;
        }

        string gameVersion = await File.ReadAllTextAsync(Path.Combine(App.UserSettings.RimWorldFolder, "Version.txt"), cancellationToken);
        gameVersion = gameVersion.Trim()[..3];

        IsBusy = true;

        IModSource[] modSources = await ModSourceManager.GetModSourcesAsync(App.UserSettings.ModSourcesUri, cancellationToken);

        if (modSources.Length == 0)
        {
            App.Log.Error("Got empty ModSources array");
            return;
        }

        ModViewModel[] viewModels = new ModViewModel[modSources.Length];

        await Parallel.ForAsync(0, modSources.Length, cancellationToken, (i,token) =>
                RequestRemoteModAsync(gameVersion, i, modSources[i], viewModels, token));

        for (int i = 0; i < modSources.Length; i++)
        {
            ModViewModel viewModel = viewModels[i];

            if (viewModel is null)
                continue;

            ViewMods.Add(viewModel);
        }

        IsBusy = false;
    }

    private static async ValueTask RequestRemoteModAsync(string gameVersion, int index, IModSource modSource, ModViewModel[] viewModels, CancellationToken cancellationToken)
    {
        RimWorldModAbout? about = await modSource.FetchAboutAsync(cancellationToken);

        if (about is null) return;

        if (!about.SupportedVersions.Contains(gameVersion))
        {
            return;
        }

        string remoteVersion;
        if (!string.IsNullOrWhiteSpace(about.ModVersion))
        {
            remoteVersion = about.ModVersion;
        }
        else
        {
            remoteVersion = await modSource.FetchVersionAsync(cancellationToken);
        }

        RimWorldMod mod = RimWorldMod.GetSingleton(about);
        mod.RemoteVersion = remoteVersion;
        viewModels[index] = new(mod, modSource);
        App.Log.Debug($"Loaded remote info for {mod.Name} {remoteVersion}");
    }
}

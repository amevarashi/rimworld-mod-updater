using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using RimWorldModUpdater.Models;

namespace RimWorldModUpdater.ViewModels;

public class ModViewModel : ViewModelBase
{

    public string Id => Mod.Id;
    public string Name => Mod.Name;
    public string Author => Mod.Author;

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => this.RaiseAndSetIfChanged(ref isSelected, value);
    }

    private bool isUpdating;
    public bool IsUpdating
    {
        get => isUpdating;
        set => this.RaiseAndSetIfChanged(ref isUpdating, value);
    }
    private float updateProgress;
    public float UpdateProgress
    {
        get => updateProgress;
        set => this.RaiseAndSetIfChanged(ref updateProgress, value);
    }

    private readonly ObservableAsPropertyHelper<string> localVersion;
    public string LocalVersion => localVersion?.Value ?? "?"; // For whatever reason this called once before property initialized

    private readonly ObservableAsPropertyHelper<string> remoteVersion;
    public string RemoteVersion => remoteVersion.Value ?? "?";

    public string Description => Mod.Description.Trim();
    public string DescriptionString => Description.Replace(Environment.NewLine, " ");
    public RimWorldMod Mod { get; }
    private IModSource Source { get; }

    public ModViewModel(RimWorldMod mod, IModSource source)
    {
        isSelected = !string.IsNullOrWhiteSpace(mod.LocalVersion) && mod.LocalVersion != "n/a" && mod.LocalVersion != mod.RemoteVersion;
        Source = source;
        Mod = mod;
        UpdateProgress = 0f;

        localVersion = this
            .WhenAnyValue(x => x.Mod.LocalVersion)
            .Select(x => string.IsNullOrWhiteSpace(x) ? "Not installed" : x)
            .ToProperty(this, x => x.LocalVersion);

        remoteVersion = this
            .WhenAnyValue(x => x.Mod.RemoteVersion)
            .Select(x => string.IsNullOrWhiteSpace(x) ? "n/a" : x)
            .ToProperty(this, x => x.RemoteVersion);
    }

    public async ValueTask UpdateAsync(CancellationToken cancellationToken)
    {
        App.Log.Information($"Downloading {Name}");
        IsUpdating = true;
        Progress<float> progress = new(x => RxApp.MainThreadScheduler.Schedule(() => UpdateProgress = x * 100f));
        await Source.DownloadAsync(Mod, progress, cancellationToken);
        IsUpdating = false;
        IsSelected = false;
    }
}

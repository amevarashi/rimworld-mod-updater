using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using ReactiveUI;
using Splat;

namespace RimWorldModUpdater.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<bool> finishedSetup;
    public bool FinishedSetup => finishedSetup.Value;

    public string RimWorldFolder
    {
        get => App.UserSettings!.RimWorldFolder;
        set => this.RaiseAndSetIfChanged(ref App.UserSettings!.rimWorldFolder, value);
    }

    private string folderError = string.Empty;
    public string FolderError
    {
        get => folderError;
        set => this.RaiseAndSetIfChanged(ref folderError, value);
    }

    private ObservableCollection<string> locales = ["en_US"];
    public ObservableCollection<string> Locales
    {
        get => locales;
        set => this.RaiseAndSetIfChanged(ref locales, value);
    }

    private int selectedLocaleIndex = 0;
    public int SelectedLocaleIndex
    {
        get => selectedLocaleIndex;
        set => this.RaiseAndSetIfChanged(ref selectedLocaleIndex, value);
    }

    private string rimWorldVersion = string.Empty;
    public string RimWorldVersion
    {
        get => rimWorldVersion;
        set => this.RaiseAndSetIfChanged(ref rimWorldVersion, value);
    }

    public string AppVersion => App.Version;

    public ICommand SaveUserSettingsCommand { get; }
    public ICommand SelectRimworldFolderCommand { get; }

    public SettingsViewModel()
    {
        SaveUserSettingsCommand = ReactiveCommand.CreateFromTask(() => App.UserSettings?.SaveAsync(App.SettingFolder)!);
        SelectRimworldFolderCommand = ReactiveCommand.CreateFromTask(SelectRimworldFolder);

        finishedSetup = this
            .WhenAnyValue(x => x.RimWorldFolder)
            .Throttle(TimeSpan.FromMilliseconds(500))
            //.ObserveOn(RxApp.MainThreadScheduler)
            .Select(par => IsRimWorldDir(par))
            .ToProperty(this, x => x.FinishedSetup);

        this.WhenAnyValue(x => x.RimWorldFolder, x => x.SelectedLocaleIndex)
            .Throttle(TimeSpan.FromSeconds(2))
            .Select(x => new Unit())
            .InvokeCommand(this, x => x.SaveUserSettingsCommand);

        selectedLocaleIndex = App.UserSettings!.ActiveLocale == "en_US" ? 0 : 1;
        //this.WhenAnyValue(x => x.SelectedLocaleIndex).Subscribe(x => App.SetLocale(x));
    }

    private bool IsRimWorldDir(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            FolderError = "This field is obligatory";
            return false;
        }

        DirectoryInfo directory = new(path);

        if (!directory.Exists)
        {
            FolderError = "Folder does not exist";
            return false;
        }

        if (!Array.Exists(directory.GetFiles(), x => x.Name == "Version.txt"))
        {
            FolderError = "Version.txt not found in the folder";
            return false;
        }

        if (!Array.Exists(directory.GetDirectories(), x => x.Name == "Mods"))
        {
            FolderError = "Mods subfolder not found in the folder";
            return false;
        }

        FolderError = string.Empty;
        RimWorldVersion = File.ReadAllText(Path.Combine(directory.FullName, "Version.txt")).Trim();
        return true;
    }

    private async Task SelectRimworldFolder(CancellationToken cancellationToken)
    {
        FolderPickerOpenOptions folderPickerOpenOptions = new() { AllowMultiple = false };
        IReadOnlyList<IStorageFolder> selected = await Locator.Current.GetService<IStorageProvider>()!.OpenFolderPickerAsync(folderPickerOpenOptions);

        if (selected.Count == 0)
        {
            return;
        }

        RimWorldFolder = selected[0].Path.LocalPath;
        _ = IsRimWorldDir(RimWorldFolder); // To update error immediately
    }
}

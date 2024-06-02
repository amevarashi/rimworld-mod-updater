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
using RimWorldModUpdater.Models;
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

    public string ModSourcesUri
    {
        get => App.UserSettings!.ModSourcesUri;
        set => this.RaiseAndSetIfChanged(ref App.UserSettings!.modSourcesUri, value);
    }

    private string modSourcesError = string.Empty;
    public string ModSourcesError
    {
        get => modSourcesError;
        set => this.RaiseAndSetIfChanged(ref modSourcesError, value);
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

    public string AppVersion => App.Version.ToString();

    public ICommand SaveUserSettingsCommand { get; }
    public ICommand SelectRimworldFolderCommand { get; }

    public SettingsViewModel()
    {
        SaveUserSettingsCommand = ReactiveCommand.CreateFromTask(() => App.UserSettings?.SaveAsync(App.SettingFolder)!);
        SelectRimworldFolderCommand = ReactiveCommand.CreateFromTask(SelectRimworldFolder);

        finishedSetup = this
            .WhenAnyValue(x => x.FolderError, y => y.ModSourcesError)
            .Select(par => string.IsNullOrWhiteSpace(par.Item1) && string.IsNullOrWhiteSpace(par.Item2))
            .ToProperty(this, x => x.FinishedSetup);

        this.WhenAnyValue(x => x.RimWorldFolder, x => x.SelectedLocaleIndex)
            .Throttle(TimeSpan.FromSeconds(2))
            .Select(x => new Unit())
            .InvokeCommand(this, x => x.SaveUserSettingsCommand);

        this.WhenAnyValue(x => x.RimWorldFolder)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(IsRimWorldDir);

        this.WhenAnyValue(x => x.ModSourcesUri)
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(CheckModSourcesUri);

        //selectedLocaleIndex = App.UserSettings!.ActiveLocale == "en_US" ? 0 : 1;
        //this.WhenAnyValue(x => x.SelectedLocaleIndex).Subscribe(x => App.SetLocale(x));
    }

    private void IsRimWorldDir(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            FolderError = "This field is obligatory";
            return;
        }

        DirectoryInfo directory = new(path);

        if (!directory.Exists)
        {
            FolderError = "Folder does not exist";
            return;
        }

        if (!Array.Exists(directory.GetFiles(), x => x.Name == "Version.txt"))
        {
            FolderError = "Version.txt not found in the folder";
            return;
        }

        if (!Array.Exists(directory.GetDirectories(), x => x.Name == "Mods"))
        {
            FolderError = "Mods subfolder not found in the folder";
            return;
        }

        FolderError = string.Empty;
        RimWorldVersion = File.ReadAllText(Path.Combine(directory.FullName, "Version.txt")).Trim();
        return;
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
        IsRimWorldDir(RimWorldFolder); // To update error immediately
    }

    private async void CheckModSourcesUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            ModSourcesError = "This field is obligatory";
            return;
        }

        if (!await ModSourceManager.IsValidModSourcesUri(uri))
        {
            ModSourcesError = "Not a valid";
            return;
        }

        ModSourcesError = string.Empty;
    }
}

using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using RimWorldModUpdater.Models;
using RimWorldModUpdater.ViewModels;
using RimWorldModUpdater.Views;
using Serilog;
using Serilog.Core;
using Splat;

#if SELFUPDATE
using System.Threading.Tasks;
#endif

namespace RimWorldModUpdater;

public partial class App : Application
{
    public static readonly string SettingFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RimWorldModUpdater");
    public static readonly Logger Log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            #if DEBUG
            .WriteTo.Console()
            #else
            .WriteTo.File("RimWorldModUpdater.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 2,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            #endif
            .CreateLogger();
    public static UserSettings? UserSettings { get; private set; }

    public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

    private ResourceDictionary? _activeLocale = null;

    public override void Initialize()
    {
        Log.Debug("Initialization");
        AvaloniaXamlLoader.Load(this);

        if (!Directory.Exists(SettingFolder))
        {
            Log.Information("Creating RimWorldModUpdater folder in AppData");
            Directory.CreateDirectory(SettingFolder);
        }

        UserSettings = LoadUserSettings();
        SetLocale(UserSettings!.ActiveLocale);
    }

    private static UserSettings LoadUserSettings()
    {
        UserSettings? userSettings = UserSettings.Load(SettingFolder);
        userSettings ??= UserSettings.Create();

        if (userSettings.RimWorldFolder == string.Empty)
        {
            userSettings.RimWorldFolder = UserSettings.TryFindRimWorldFolder();
        }
        return userSettings;
    }

    public static void SetLocale(string localeKey)
    {
        var app = Current as App;
        ResourceDictionary? targetLocale = app!.Resources[localeKey] as ResourceDictionary;
        if (targetLocale == null || targetLocale == app._activeLocale)
        {
            return;
        }

        if (app._activeLocale != null)
        {
            app.Resources.MergedDictionaries.Remove(app._activeLocale);
        }

        app.Resources.MergedDictionaries.Add(targetLocale);
        app._activeLocale = targetLocale;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Self-update works only on assembly published with PublishSingleFile
            #if SELFUPDATE
            Task<string?> checkUpdates = Task.Run(async () => await SelfUpdateManager.CheckUpdates());
            string? downloadUri = checkUpdates.GetAwaiter().GetResult();

            if (downloadUri is not null)
            {
                desktop.MainWindow = new UpdateWindow
                {
                    DataContext = new UpdateWindowViewModel(downloadUri),
                };
            }
            else
            {
            #endif
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(_activeLocale!),
                };
                Locator.CurrentMutable.RegisterConstant(desktop.MainWindow.StorageProvider, typeof(IStorageProvider));
            #if SELFUPDATE
            }
            #endif
        }

        base.OnFrameworkInitializationCompleted();
    }
}
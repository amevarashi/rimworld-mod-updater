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

namespace RimWorldModUpdater;

public partial class App : Application
{
    public static readonly string SettingFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RimWorldModUpdater");
    public static readonly Logger Log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            //.WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    public static UserSettings? UserSettings { get; private set; }

    public static string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "fail";

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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_activeLocale!),
            };
            Locator.CurrentMutable.RegisterConstant(desktop.MainWindow.StorageProvider, typeof(IStorageProvider));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using RimWorldModUpdater.Models;

namespace RimWorldModUpdater.ViewModels;

public class UpdateWindowViewModel : ViewModelBase
{
    public string Title { get; }

    private float downloadProgress;
    public float DownloadProgress
    {
        get => downloadProgress;
        set => this.RaiseAndSetIfChanged(ref downloadProgress, value);
    }

    public UpdateWindowViewModel(string downloadUri)
    {
        Title = "test";
        DownloadProgress = 0f;
        RxApp.MainThreadScheduler.ScheduleAsync((sch, token) => UpdateAsync(downloadUri, token));
    }

    public async Task UpdateAsync(string downloadUri, CancellationToken cancellationToken)
    {
        App.Log.Information($"Downloading new version");
        Progress<float> progress = new(x => RxApp.MainThreadScheduler.Schedule(() => DownloadProgress = x * 100f));
        Process currentProcess = Process.GetCurrentProcess();
        string appFileName = currentProcess.MainModule!.FileName;
        await SelfUpdateManager.DownloadAsync(downloadUri, appFileName, progress, cancellationToken);
        LaunchNewVersion(appFileName);

        App.Log.Information($"Killing current process");
        currentProcess.Kill();
    }

    private static void LaunchNewVersion(string fullName)
    {
        App.Log.Information($"Launching {fullName}");

        ProcessStartInfo startInfo = new()
        {
            FileName = fullName,
            WorkingDirectory = Path.GetDirectoryName(fullName)
        };
        Process process = new()
        {
            StartInfo = startInfo
        };

        bool started = process.Start();

        if (started)
        {
            App.Log.Information($"New process started");
        }
        else
        {
            App.Log.Information($"New process not started");
        }
    }
}

using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace RimWorldModUpdater.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ObservableAsPropertyHelper<bool> finishedSetup;
    public bool FinishedSetup => finishedSetup.Value;

    public string Title { get; }

    public ModListViewModel? ModListViewModel { get; private set; }
    public SettingsViewModel SettingsViewModel { get; }

    public ICommand ActivateModListView { get; }

    public MainWindowViewModel(ResourceDictionary translations)
    {
        Title = $"{translations["Text.WindowTitle"]} - {App.Version}";

        ActivateModListView = ReactiveCommand.Create(() => ModListViewModel ??= new());

        SettingsViewModel = new();

        finishedSetup = this
            .WhenAnyValue(x => x.SettingsViewModel.FinishedSetup)
            .ToProperty(this, x => x.FinishedSetup);

        this.WhenAnyValue(x => x.FinishedSetup)
            .Where(x => x == true)
            .Select(x => new Unit())
            .InvokeCommand(ActivateModListView);
    }
}

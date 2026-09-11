using System.Windows;
using RePKG.App.Localization;
using RePKG.App.Services;
using RePKG.App.ViewModels;
using RePKG.App.Views;
using RePKG.Core.Scanning;

namespace RePKG.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var localization = LocalizationService.Current;
        localization.InitializeForCurrentCulture();
        var settingsStore = new JsonSettingsStore();
        var mainViewModel = new MainViewModel(
            new ProjectScanner(),
            settingsStore,
            new SteamLocator(),
            new ExplorerService(),
            new UserDialogService(),
            new OutputProjectService(),
            new ThemeService(),
            new BackgroundService(),
            localization);
        var mainWindow = new MainWindow(mainViewModel);
        mainWindow.Loaded += async (_, _) => await mainViewModel.InitializeAsync();

        MainWindow = mainWindow;
        mainWindow.Show();
    }
}

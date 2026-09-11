using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RePKG.App.Localization;
using RePKG.App.Services;
using RePKG.Core.Abstractions;
using RePKG.Core.Extraction;
using RePKG.Core.Models;

namespace RePKG.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IProjectScanner _scanner;
    private readonly ISettingsStore _settingsStore;
    private readonly SteamLocator _steamLocator;
    private readonly ExplorerService _explorerService;
    private readonly UserDialogService _dialogService;
    private readonly OutputProjectService _outputProjectService;
    private readonly ThemeService _themeService;
    private readonly BackgroundService _backgroundService;
    private readonly LocalizationService _localization;
    private CancellationTokenSource? _operationSource;
    private string _inputPath = string.Empty;
    private string _outputPath = string.Empty;
    private string _rePkgPath = string.Empty;
    private string _searchText = string.Empty;
    private string _selectedType = "All";
    private string _statusKey = "Status.LoadingSettings";
    private string _statusEnglishDefault = "Loading settings...";
    private object?[] _statusArguments = [];
    private string _language = LocalizationService.EnglishLanguage;
    private string _currentItem = string.Empty;
    private bool _recursiveScan = true;
    private bool _convertTex = true;
    private bool _copyProject = true;
    private bool _overwrite;
    private bool _copyPreview = true;
    private bool _isBusy;
    private bool _isOutputView;
    private double _progressValue;
    private string _themeMode = "Dark";
    private string _backgroundImagePath = string.Empty;
    private double _backgroundOpacity = 0.35;

    public MainViewModel(
        IProjectScanner scanner,
        ISettingsStore settingsStore,
        SteamLocator steamLocator,
        ExplorerService explorerService,
        UserDialogService dialogService,
        OutputProjectService outputProjectService,
        ThemeService themeService,
        BackgroundService backgroundService,
        LocalizationService localization)
    {
        _scanner = scanner;
        _settingsStore = settingsStore;
        _steamLocator = steamLocator;
        _explorerService = explorerService;
        _dialogService = dialogService;
        _outputProjectService = outputProjectService;
        _themeService = themeService;
        _backgroundService = backgroundService;
        _localization = localization;

        ProjectTypes =
        [
            new("All", "ProjectType.All", "All", localization),
            new("Scene", "ProjectType.Scene", "Scene", localization),
            new("Video", "ProjectType.Video", "Video", localization),
            new("Web", "ProjectType.Web", "Web", localization),
            new("Application", "ProjectType.Application", "Application", localization),
        ];
        ThemeChoices =
        [
            new("Light", "Theme.Light", "Light", localization),
            new("Dark", "Theme.Dark", "Dark", localization),
            new("Custom", "Theme.Custom", "Custom background", localization),
        ];
        LanguageChoices = localization.DiscoverLanguages();

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterProject;
        ScanCommand = new AsyncRelayCommand(ScanCurrentAsync, CanStartOperation);
        ExtractSelectedCommand = new AsyncRelayCommand(ExtractSelectedAsync, CanExtract);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync, CanDelete);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        DetectSteamCommand = new RelayCommand(DetectSteam, () => !IsBusy);
        BrowseInputCommand = new RelayCommand(BrowseInput, () => !IsBusy);
        BrowseOutputCommand = new RelayCommand(BrowseOutput, () => !IsBusy);
        BrowseRePkgCommand = new RelayCommand(BrowseRePkg, () => !IsBusy);
        BrowseBackgroundCommand = new AsyncRelayCommand(BrowseBackgroundAsync, () => !IsBusy);
        ClearBackgroundCommand = new RelayCommand(ClearBackground, () => !IsBusy && !string.IsNullOrWhiteSpace(BackgroundImagePath));
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        SelectAllCommand = new RelayCommand(SelectAll, CanSelectAll);
        ClearSelectionCommand = new RelayCommand(ClearSelection, CanClearSelection);
        SwitchViewCommand = new AsyncRelayCommand<string>(SwitchViewAsync, _ => !IsBusy);
        OpenLocationCommand = new RelayCommand<ProjectItemViewModel>(OpenLocation);
        ExtractOneCommand = new AsyncRelayCommand<ProjectItemViewModel>(ExtractOneAsync, CanExtractOne);
        DeleteOneCommand = new AsyncRelayCommand<ProjectItemViewModel>(DeleteOneAsync, CanDeleteOne);
    }

    public ObservableCollection<ProjectItemViewModel> Items { get; } = [];

    public ObservableCollection<string> Logs { get; } = [];

    public ICollectionView ItemsView { get; }

    public IReadOnlyList<LocalizedOption> ProjectTypes { get; }

    public IReadOnlyList<LocalizedOption> ThemeChoices { get; }

    public IReadOnlyList<LanguageDefinition> LanguageChoices { get; }

    public IAsyncRelayCommand ScanCommand { get; }

    public IAsyncRelayCommand ExtractSelectedCommand { get; }

    public IAsyncRelayCommand DeleteSelectedCommand { get; }

    public IAsyncRelayCommand SaveSettingsCommand { get; }

    public IRelayCommand DetectSteamCommand { get; }

    public IRelayCommand BrowseInputCommand { get; }

    public IRelayCommand BrowseOutputCommand { get; }

    public IRelayCommand BrowseRePkgCommand { get; }

    public IAsyncRelayCommand BrowseBackgroundCommand { get; }

    public IRelayCommand ClearBackgroundCommand { get; }

    public IRelayCommand CancelCommand { get; }

    public IRelayCommand SelectAllCommand { get; }

    public IRelayCommand ClearSelectionCommand { get; }

    public IAsyncRelayCommand<string> SwitchViewCommand { get; }

    public IRelayCommand<ProjectItemViewModel> OpenLocationCommand { get; }

    public IAsyncRelayCommand<ProjectItemViewModel> ExtractOneCommand { get; }

    public IAsyncRelayCommand<ProjectItemViewModel> DeleteOneCommand { get; }

    public string InputPath
    {
        get => _inputPath;
        set => SetProperty(ref _inputPath, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string RePkgPath
    {
        get => _rePkgPath;
        set => SetProperty(ref _rePkgPath, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ItemsView.Refresh();
                NotifySelectionState();
            }
        }
    }

    public string SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                ItemsView.Refresh();
                NotifySelectionState();
            }
        }
    }

    public string Language
    {
        get => _language;
        set
        {
            var previousLocalizationLanguage = _localization.CurrentLanguage;
            _localization.SetLanguage(value);
            var normalized = _localization.CurrentLanguage;
            var viewModelLanguageChanged = SetProperty(ref _language, normalized);
            var localizationLanguageChanged = !string.Equals(
                previousLocalizationLanguage,
                normalized,
                StringComparison.OrdinalIgnoreCase);
            if (viewModelLanguageChanged || localizationLanguageChanged)
            {
                OnPropertyChanged(nameof(ViewTitle));
                OnPropertyChanged(nameof(SelectionSummary));
                OnPropertyChanged(nameof(StatusMessage));
                foreach (var item in Items)
                {
                    item.RefreshLocalization();
                }
            }
        }
    }

    public string StatusMessage
    {
        get => _localization.Format(_statusKey, _statusEnglishDefault, _statusArguments);
    }

    public string CurrentItem
    {
        get => _currentItem;
        private set => SetProperty(ref _currentItem, value);
    }

    public bool RecursiveScan
    {
        get => _recursiveScan;
        set => SetProperty(ref _recursiveScan, value);
    }

    public bool ConvertTex
    {
        get => _convertTex;
        set => SetProperty(ref _convertTex, value);
    }

    public bool CopyProject
    {
        get => _copyProject;
        set => SetProperty(ref _copyProject, value);
    }

    public bool Overwrite
    {
        get => _overwrite;
        set => SetProperty(ref _overwrite, value);
    }

    public bool CopyPreview
    {
        get => _copyPreview;
        set => SetProperty(ref _copyPreview, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommands();
            }
        }
    }

    public bool IsOutputView
    {
        get => _isOutputView;
        private set
        {
            if (SetProperty(ref _isOutputView, value))
            {
                OnPropertyChanged(nameof(IsInputView));
                OnPropertyChanged(nameof(ViewTitle));
                NotifyCommands();
            }
        }
    }

    public bool IsInputView => !IsOutputView;

    public string ViewTitle => IsOutputView
        ? L("View.OutputProjects", "Output projects")
        : L("View.WorkshopProjects", "Workshop projects");

    public int SelectedCount => Items.Count(item => item.IsSelected);

    public string SelectionSummary => SelectedCount == 0
        ? L("Selection.None", "No projects selected")
        : LF("Selection.Count", "{0} selected", SelectedCount);

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string ThemeMode
    {
        get => _themeMode;
        set
        {
            var normalized = value is "Light" or "Custom" ? value : "Dark";
            if (SetProperty(ref _themeMode, normalized))
            {
                _themeService.Apply(normalized);
                OnPropertyChanged(nameof(IsCustomBackground));
            }
        }
    }

    public string BackgroundImagePath
    {
        get => _backgroundImagePath;
        private set
        {
            if (SetProperty(ref _backgroundImagePath, value))
            {
                OnPropertyChanged(nameof(IsCustomBackground));
                ClearBackgroundCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set => SetProperty(ref _backgroundOpacity, Math.Clamp(value, 0.1, 1));
    }

    public bool IsCustomBackground => ThemeMode == "Custom" && File.Exists(BackgroundImagePath);

    public async Task InitializeAsync()
    {
        try
        {
            ApplySettings(await _settingsStore.LoadAsync());
            SetStatus("Status.Ready", "Ready");
            if (Directory.Exists(InputPath))
            {
                await ScanCurrentAsync();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetStatus("Status.SettingsLoadFailed", "Failed to load settings");
            AddLog(LF("Log.SettingsLoadFailed", "Failed to load settings: {0}", exception.Message));
        }
    }

    public async Task ImportAndExtractDroppedFoldersAsync(IReadOnlyList<string> paths)
    {
        if (IsBusy)
        {
            return;
        }

        var directories = paths
            .Where(Directory.Exists)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (directories.Length == 0)
        {
            _dialogService.ShowError(L("Drop.FoldersOnly", "Drop one or more project folders."));
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputPath))
        {
            _dialogService.ShowError(L("Drop.OutputRequired", "Select an output directory before using quick extraction."));
            return;
        }

        BeginOperation("Drop.Scanning", "Reading dropped folders...");
        var projects = new Dictionary<string, ProjectItem>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var directory in directories)
            {
                var result = await _scanner.ScanAsync(directory, true, _operationSource!.Token);
                foreach (var project in result.Projects)
                {
                    projects[project.DirectoryPath] = project;
                }

                foreach (var warning in result.Warnings)
                {
                    AddLog(LF("Log.ScanWarning", "Scan warning [{0}]: {1}", warning.Path, warning.Message));
                }
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Status.ScanCanceled", "Scan canceled");
            return;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            SetStatus("Status.ScanFailed", "Scan failed");
            _dialogService.ShowError(exception.Message);
            return;
        }
        finally
        {
            EndOperation();
        }

        if (projects.Count == 0)
        {
            SetStatus("Drop.NoProjects", "No valid projects were found in the dropped folders.");
            return;
        }

        IsOutputView = false;
        SearchText = string.Empty;
        SelectedType = "All";
        ClearItems();
        foreach (var project in projects.Values.OrderBy(project => project.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            var item = new ProjectItemViewModel(project) { IsSelected = true };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }

        if (directories.Length == 1)
        {
            InputPath = directories[0];
        }

        ItemsView.Refresh();
        NotifySelectionState();
        await ExtractProjectsAsync(projects.Values.ToArray());
    }

    private async Task ScanCurrentAsync()
    {
        var path = IsOutputView ? OutputPath : InputPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            _dialogService.ShowError(L("Error.InvalidScanDirectory", "Select a valid directory to scan."));
            return;
        }

        BeginOperation("Status.Scanning", "Scanning...");
        try
        {
            var result = await _scanner.ScanAsync(path, IsOutputView || RecursiveScan, _operationSource!.Token);
            ClearItems();
            foreach (var project in result.Projects)
            {
                var item = new ProjectItemViewModel(project);
                item.PropertyChanged += OnItemPropertyChanged;
                Items.Add(item);
            }

            ItemsView.Refresh();
            NotifySelectionState();
            SetStatus("Status.ScanComplete", "Found {0} projects, {1} total", Items.Count, FormatSize(result.TotalSize));
            foreach (var warning in result.Warnings)
            {
                AddLog(LF("Log.ScanWarning", "Scan warning [{0}]: {1}", warning.Path, warning.Message));
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Status.ScanCanceled", "Scan canceled");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            SetStatus("Status.ScanFailed", "Scan failed");
            _dialogService.ShowError(exception.Message);
        }
        finally
        {
            EndOperation();
        }
    }

    private Task ExtractSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).Select(item => item.Project).ToArray();
        return ExtractProjectsAsync(selected);
    }

    private Task ExtractOneAsync(ProjectItemViewModel? item)
    {
        return item is null ? Task.CompletedTask : ExtractProjectsAsync([item.Project]);
    }

    private async Task ExtractProjectsAsync(ProjectItem[] selected)
    {
        if (selected.Length == 0 || string.IsNullOrWhiteSpace(OutputPath))
        {
            return;
        }

        if (selected.Any(project => project.Type == ProjectType.Scene) && !File.Exists(RePkgPath))
        {
            _dialogService.ShowError(L("Error.InvalidRePkg", "The selection contains scene projects, but the RePKG.exe path is invalid."));
            return;
        }

        Directory.CreateDirectory(OutputPath);
        BeginOperation("Status.Processing", "Processing projects...");
        Logs.Clear();
        var exporter = new ProjectExporter(RePkgPath);
        var completed = 0;
        var succeeded = 0;
        try
        {
            foreach (var project in selected)
            {
                _operationSource!.Token.ThrowIfCancellationRequested();
                CurrentItem = project.Title;
                AddLog(LF("Log.Started", "Started: {0} ({1})", project.Title, project.Type));
                var detailProgress = new Progress<ExtractionProgress>(detail =>
                {
                    if (!string.IsNullOrWhiteSpace(detail.Message))
                    {
                        AddLog(detail.Message);
                    }
                });
                var result = await exporter.ExtractAsync(
                    project,
                    OutputPath,
                    BuildExtractionOptions(),
                    detailProgress,
                    _operationSource.Token);
                completed++;
                ProgressValue = (double)completed / selected.Length * 100;
                if (result.IsSuccess)
                {
                    succeeded++;
                    AddLog(LF("Log.Succeeded", "Succeeded: {0} ({1:0.00} seconds)", project.Title, result.Elapsed.TotalSeconds));
                }
                else
                {
                    AddLog(LF("Log.Failed", "Failed: {0}: {1}", project.Title, result.ErrorMessage));
                }
            }

            SetStatus("Status.ProcessComplete", "Complete: {0} succeeded, {1} failed", succeeded, selected.Length - succeeded);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Status.TaskCanceled", "Task canceled after {0}/{1}", completed, selected.Length);
            AddLog(L("Log.TaskCanceled", "The task was canceled by the user."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetStatus("Status.ProcessFailed", "Processing failed");
            AddLog(exception.Message);
            _dialogService.ShowError(exception.Message);
        }
        finally
        {
            CurrentItem = string.Empty;
            EndOperation();
        }
    }

    private Task DeleteSelectedAsync()
    {
        var selected = Items.Where(item => item.IsSelected).ToArray();
        return DeleteItemsAsync(selected);
    }

    private Task DeleteOneAsync(ProjectItemViewModel? item)
    {
        return item is null ? Task.CompletedTask : DeleteItemsAsync([item]);
    }

    private async Task DeleteItemsAsync(ProjectItemViewModel[] selected)
    {
        if (selected.Length == 0
            || !_dialogService.Confirm(
                LF("Dialog.DeleteMessage", "Permanently delete {0} projects from this output root?\n{1}\n\nThis action cannot be undone.", selected.Length, OutputPath),
                L("Dialog.DeleteTitle", "Confirm deletion")))
        {
            return;
        }

        BeginOperation("Status.Deleting", "Deleting...");
        try
        {
            var deleted = await _outputProjectService.DeleteAsync(
                OutputPath,
                selected.Select(item => item.Project).ToArray(),
                _operationSource!.Token);
            foreach (var item in selected)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                Items.Remove(item);
            }

            ItemsView.Refresh();
            NotifySelectionState();
            SetStatus("Status.DeleteComplete", "Deleted {0} projects; {1} remaining", deleted, Items.Count);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _dialogService.ShowError(exception.Message);
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsStore.SaveAsync(BuildSettings());
            SetStatus("Status.SettingsSaved", "Settings saved");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _dialogService.ShowError(LF("Error.SettingsSaveFailed", "Failed to save settings: {0}", exception.Message));
        }
    }

    private async Task SwitchViewAsync(string? view)
    {
        IsOutputView = string.Equals(view, "output", StringComparison.OrdinalIgnoreCase);
        SearchText = string.Empty;
        SelectedType = "All";
        ClearItems();
        if ((IsOutputView ? Directory.Exists(OutputPath) : Directory.Exists(InputPath)))
        {
            await ScanCurrentAsync();
        }
        else
        {
            if (IsOutputView)
            {
                SetStatus("Status.SelectOutputDirectory", "Select an output directory");
            }
            else
            {
                SetStatus("Status.SelectInputDirectory", "Select an input directory");
            }
        }
    }

    private void DetectSteam()
    {
        var result = _steamLocator.Detect();
        if (result.WorkshopPath is not null)
        {
            InputPath = result.WorkshopPath;
        }

        if (result.MyProjectsPath is not null)
        {
            OutputPath = result.MyProjectsPath;
        }

        if (result.WallpaperEngineFound)
        {
            SetStatus("Status.WallpaperEngineFound", "Wallpaper Engine detected");
        }
        else
        {
            SetStatus("Status.WallpaperEngineMissing", "Steam was detected, but Wallpaper Engine was not found");
        }
    }

    private void BrowseInput()
    {
        InputPath = _dialogService.PickFolder(L("Dialog.SelectWorkshopDirectory", "Select the Wallpaper Engine workshop directory"), InputPath) ?? InputPath;
    }

    private void BrowseOutput()
    {
        OutputPath = _dialogService.PickFolder(L("Dialog.SelectOutputDirectory", "Select an output directory"), OutputPath) ?? OutputPath;
    }

    private void BrowseRePkg()
    {
        RePkgPath = _dialogService.PickExecutable(L("Dialog.SelectRePkg", "Select RePKG.exe"), RePkgPath) ?? RePkgPath;
    }

    private async Task BrowseBackgroundAsync()
    {
        var selected = _dialogService.PickImage(L("Dialog.SelectBackground", "Select a custom background"), BackgroundImagePath);
        if (selected is null)
        {
            return;
        }

        try
        {
            BackgroundImagePath = await _backgroundService.ImportAsync(selected);
            ThemeMode = "Custom";
            SetStatus("Status.BackgroundApplied", "Custom background applied. Save settings to keep it for the next launch.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _dialogService.ShowError(exception.Message);
        }
    }

    private void ClearBackground()
    {
        BackgroundImagePath = string.Empty;
        ThemeMode = "Dark";
    }

    private void OpenLocation(ProjectItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            _explorerService.OpenLocation(item.DirectoryPath);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _dialogService.ShowError(exception.Message);
        }
    }

    private void SelectAll()
    {
        foreach (ProjectItemViewModel item in ItemsView)
        {
            item.IsSelected = true;
        }
    }

    private void ClearSelection()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
    }

    private void ClearItems()
    {
        foreach (var item in Items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        Items.Clear();
        ItemsView.Refresh();
        NotifySelectionState();
    }

    private void Cancel()
    {
        _operationSource?.Cancel();
    }

    private bool FilterProject(object candidate)
    {
        if (candidate is not ProjectItemViewModel item)
        {
            return false;
        }

        var matchesText = string.IsNullOrWhiteSpace(SearchText)
            || item.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
            || item.DirectoryPath.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        var matchesType = SelectedType == "All"
            || string.Equals(item.Project.Type.ToString(), SelectedType, StringComparison.OrdinalIgnoreCase);
        return matchesText && matchesType;
    }

    private void ApplySettings(AppSettings settings)
    {
        Language = string.IsNullOrWhiteSpace(settings.Language)
            ? _localization.CurrentLanguage
            : settings.Language;
        InputPath = settings.InputPath;
        OutputPath = settings.OutputPath;
        RePkgPath = RePkgPathResolver.Resolve(settings.RePkgPath);
        RecursiveScan = settings.RecursiveScan;
        ConvertTex = settings.Extraction.ConvertTex;
        CopyProject = settings.Extraction.CopyProject;
        Overwrite = settings.Extraction.Overwrite;
        CopyPreview = settings.Extraction.CopyPreview;
        BackgroundImagePath = settings.Appearance.BackgroundImagePath;
        BackgroundOpacity = settings.Appearance.BackgroundOpacity;
        ThemeMode = settings.Appearance.Theme;
    }

    private AppSettings BuildSettings()
    {
        return new AppSettings
        {
            Language = Language,
            InputPath = InputPath,
            OutputPath = OutputPath,
            RePkgPath = RePkgPath,
            RecursiveScan = RecursiveScan,
            Extraction = BuildExtractionOptions(),
            Appearance = new AppearanceSettings
            {
                Theme = ThemeMode,
                BackgroundImagePath = BackgroundImagePath,
                BackgroundOpacity = BackgroundOpacity,
            },
        };
    }

    private ExtractionOptions BuildExtractionOptions()
    {
        return new ExtractionOptions(ConvertTex, CopyProject, Overwrite, CopyPreview);
    }

    private bool CanStartOperation()
    {
        return !IsBusy;
    }

    private bool CanExtract()
    {
        return !IsBusy && !IsOutputView && Items.Any(item => item.IsSelected);
    }

    private bool CanDelete()
    {
        return !IsBusy && IsOutputView && Items.Any(item => item.IsSelected);
    }

    private bool CanExtractOne(ProjectItemViewModel? item)
    {
        return !IsBusy && !IsOutputView && item is not null;
    }

    private bool CanDeleteOne(ProjectItemViewModel? item)
    {
        return !IsBusy && IsOutputView && item is not null;
    }

    private bool CanSelectAll()
    {
        return !IsBusy && ItemsView.Cast<ProjectItemViewModel>().Any(item => !item.IsSelected);
    }

    private bool CanClearSelection()
    {
        return !IsBusy && Items.Any(item => item.IsSelected);
    }

    private void BeginOperation(string statusKey, string englishDefault)
    {
        _operationSource?.Dispose();
        _operationSource = new CancellationTokenSource();
        IsBusy = true;
        ProgressValue = 0;
        SetStatus(statusKey, englishDefault);
    }

    private void EndOperation()
    {
        IsBusy = false;
        _operationSource?.Dispose();
        _operationSource = null;
    }

    private void AddLog(string message)
    {
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        while (Logs.Count > 500)
        {
            Logs.RemoveAt(0);
        }
    }

    private void SetStatus(string key, string englishDefault, params object?[] arguments)
    {
        _statusKey = key;
        _statusEnglishDefault = englishDefault;
        _statusArguments = arguments;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(ProjectItemViewModel.IsSelected))
        {
            NotifySelectionState();
        }
    }

    private void NotifySelectionState()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectionSummary));
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        ExtractSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        ExtractOneCommand.NotifyCanExecuteChanged();
        DeleteOneCommand.NotifyCanExecuteChanged();
    }

    private void NotifyCommands()
    {
        ScanCommand.NotifyCanExecuteChanged();
        ExtractSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        ExtractOneCommand.NotifyCanExecuteChanged();
        DeleteOneCommand.NotifyCanExecuteChanged();
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        DetectSteamCommand.NotifyCanExecuteChanged();
        BrowseInputCommand.NotifyCanExecuteChanged();
        BrowseOutputCommand.NotifyCanExecuteChanged();
        BrowseRePkgCommand.NotifyCanExecuteChanged();
        BrowseBackgroundCommand.NotifyCanExecuteChanged();
        ClearBackgroundCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SwitchViewCommand.NotifyCanExecuteChanged();
    }

    private static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)size;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }

    private string L(string key, string englishDefault) => _localization.Get(key, englishDefault);

    private string LF(string key, string englishDefault, params object?[] arguments) =>
        _localization.Format(key, englishDefault, arguments);
}

using CommunityToolkit.Mvvm.ComponentModel;
using RePKG.App.Localization;
using RePKG.Core.Models;

namespace RePKG.App.ViewModels;

public sealed class ProjectItemViewModel(ProjectItem project) : ObservableObject
{
    private bool _isSelected;

    public ProjectItem Project { get; } = project;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Title => Project.Title;

    public string DirectoryPath => Project.DirectoryPath;

    public string? PreviewPath => Project.PreviewPath;

    public Uri? VideoSource => Project.VideoPath is { } path && File.Exists(path)
        ? new Uri(path, UriKind.Absolute)
        : null;

    public bool HasVideoPreview => VideoSource is not null;

    public string TypeName => Project.Type switch
    {
        ProjectType.Scene => LocalizationService.Current.Get("ProjectType.Scene", "Scene"),
        ProjectType.Video => LocalizationService.Current.Get("ProjectType.Video", "Video"),
        ProjectType.Web => LocalizationService.Current.Get("ProjectType.Web", "Web"),
        ProjectType.Application => LocalizationService.Current.Get("ProjectType.Application", "Application"),
        _ => Project.Type.ToString(),
    };

    public string SizeText => FormatSize(Project.Size);

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(TypeName));
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
}

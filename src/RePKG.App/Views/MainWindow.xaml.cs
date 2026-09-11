using System.Windows;
using System.Windows.Controls;
using RePKG.App.ViewModels;

namespace RePKG.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnPreviewDragEnter(object sender, DragEventArgs eventArgs)
    {
        UpdateDropState(eventArgs);
    }

    private void OnPreviewDragOver(object sender, DragEventArgs eventArgs)
    {
        UpdateDropState(eventArgs);
    }

    private void OnPreviewDragLeave(object sender, DragEventArgs eventArgs)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
    }

    private async void OnPreviewDrop(object sender, DragEventArgs eventArgs)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        eventArgs.Handled = true;
        var directories = GetDroppedDirectories(eventArgs);
        if (directories.Length > 0)
        {
            await _viewModel.ImportAndExtractDroppedFoldersAsync(directories);
        }
    }

    private void UpdateDropState(DragEventArgs eventArgs)
    {
        var canDrop = !_viewModel.IsBusy && GetDroppedDirectories(eventArgs).Length > 0;
        eventArgs.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
        eventArgs.Handled = true;
        DropOverlay.Visibility = canDrop ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string[] GetDroppedDirectories(DragEventArgs eventArgs)
    {
        return eventArgs.Data.GetDataPresent(DataFormats.FileDrop)
            && eventArgs.Data.GetData(DataFormats.FileDrop) is string[] paths
            ? paths.Where(Directory.Exists).ToArray()
            : [];
    }

    private void OnPreviewMediaOpened(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is MediaElement media)
        {
            media.Play();
        }
    }

    private void OnPreviewMediaEnded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is MediaElement media)
        {
            media.Position = TimeSpan.Zero;
            media.Play();
        }
    }

    private void OnPreviewMediaFailed(object sender, ExceptionRoutedEventArgs eventArgs)
    {
        if (sender is MediaElement media)
        {
            media.Close();
            media.Visibility = Visibility.Collapsed;
        }
    }

    private void OnPreviewMediaUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is MediaElement media)
        {
            media.Close();
        }
    }
}

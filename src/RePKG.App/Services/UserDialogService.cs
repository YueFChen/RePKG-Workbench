using System.Windows;
using Microsoft.Win32;
using RePKG.App.Localization;

namespace RePKG.App.Services;

public sealed class UserDialogService
{
    public string? PickFolder(string title, string? initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = Directory.Exists(initialDirectory) ? initialDirectory : null,
            Multiselect = false,
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? PickExecutable(string title, string? initialPath)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = LocalizationService.Current.Get(
                "Dialog.ExecutableFilter",
                "Executable files (*.exe)|*.exe|All files (*.*)|*.*"),
            FileName = File.Exists(initialPath) ? initialPath : null,
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickImage(string title, string? initialPath)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = LocalizationService.Current.Get(
                "Dialog.ImageFilter",
                "Image files|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All files (*.*)|*.*"),
            FileName = File.Exists(initialPath) ? initialPath : null,
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool Confirm(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(message, "RePKG Workbench", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

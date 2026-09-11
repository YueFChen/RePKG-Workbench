using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RePKG.App.Localization;

public sealed class LocalizedOption : INotifyPropertyChanged
{
    private readonly LocalizationService _localization;
    private readonly string _key;
    private readonly string _englishDefault;

    public LocalizedOption(string value, string key, string englishDefault, LocalizationService? localization = null)
    {
        Value = value;
        _key = key;
        _englishDefault = englishDefault;
        _localization = localization ?? LocalizationService.Current;
        _localization.PropertyChanged += OnLocalizationChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Value { get; }

    public string DisplayName => _localization.Get(_key, _englishDefault);

    public override string ToString() => DisplayName;

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is "Item[]" or nameof(LocalizationService.CurrentLanguage))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }
    }
}

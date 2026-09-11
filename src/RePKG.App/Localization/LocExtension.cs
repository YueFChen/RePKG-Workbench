using System.Windows.Data;
using System.Windows.Markup;

namespace RePKG.App.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public string Default { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var fallback = string.IsNullOrWhiteSpace(Default) ? Key : Default;
        LocalizationService.Current.RegisterDefault(Key, fallback);
        return new Binding($"[{Key}]")
        {
            Source = LocalizationService.Current,
            Mode = BindingMode.OneWay,
        }.ProvideValue(serviceProvider);
    }
}

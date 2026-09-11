using System.Windows;
using System.Windows.Media;

namespace RePKG.App.Services;

public sealed class ThemeService
{
    public void Apply(string theme)
    {
        var resources = Application.Current.Resources;
        if (string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase))
        {
            Set(resources, "WindowBrush", "#FFF1F5F9");
            Set(resources, "WindowOverlayBrush", "#FFF1F5F9");
            Set(resources, "PanelBrush", "#FFFFFFFF");
            Set(resources, "PanelTranslucentBrush", "#F7FFFFFF");
            Set(resources, "CardBrush", "#FFFFFFFF");
            Set(resources, "CardHoverBrush", "#FFEFF6FF");
            Set(resources, "InputBrush", "#FFF8FAFC");
            Set(resources, "BorderBrush", "#FFCBD5E1");
            Set(resources, "HeadingTextBrush", "#FF101C31");
            Set(resources, "PrimaryTextBrush", "#FF1B2940");
            Set(resources, "SecondaryTextBrush", "#FF52647A");
            Set(resources, "MutedTextBrush", "#FF77879A");
            Set(resources, "DisabledTextBrush", "#FFA0ADBD");
            return;
        }

        var custom = string.Equals(theme, "Custom", StringComparison.OrdinalIgnoreCase);
        Set(resources, "WindowBrush", "#FF0B1220");
        Set(resources, "WindowOverlayBrush", custom ? "#87081222" : "#FF0B1220");
        Set(resources, "PanelBrush", "#FF111C2F");
        Set(resources, "PanelTranslucentBrush", custom ? "#DD111C2F" : "#FF111C2F");
        Set(resources, "CardBrush", custom ? "#E61B2940" : "#FF1B2940");
        Set(resources, "CardHoverBrush", "#FF243653");
        Set(resources, "InputBrush", custom ? "#D9152238" : "#FF152238");
        Set(resources, "BorderBrush", "#FF34465F");
        Set(resources, "HeadingTextBrush", "#FFF3F7FC");
        Set(resources, "PrimaryTextBrush", "#FFE7EEF8");
        Set(resources, "SecondaryTextBrush", "#FFB0BED0");
        Set(resources, "MutedTextBrush", "#FF8291A6");
        Set(resources, "DisabledTextBrush", "#FF718198");
    }

    private static void Set(ResourceDictionary resources, string key, string color)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}

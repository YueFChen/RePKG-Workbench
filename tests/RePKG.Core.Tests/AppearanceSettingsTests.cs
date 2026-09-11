using System.Text.Json;
using RePKG.Core.Models;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class AppearanceSettingsTests
{
    [TestMethod]
    public void Defaults_UseReadableDarkTheme()
    {
        var settings = new AppearanceSettings();

        Assert.AreEqual("Dark", settings.Theme);
        Assert.AreEqual(string.Empty, settings.BackgroundImagePath);
        Assert.AreEqual(0.35d, settings.BackgroundOpacity);
    }

    [TestMethod]
    public void AppSettings_RoundTripsCustomAppearance()
    {
        var source = new AppSettings
        {
            Language = "zh-CN",
            Appearance = new AppearanceSettings
            {
                Theme = "Custom",
                BackgroundImagePath = @"C:\Images\wallpaper.png",
                BackgroundOpacity = 0.52,
            },
        };

        var json = JsonSerializer.Serialize(source);
        var result = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.IsNotNull(result);
        Assert.AreEqual(source.Language, result.Language);
        Assert.AreEqual(source.Appearance, result.Appearance);
    }
}

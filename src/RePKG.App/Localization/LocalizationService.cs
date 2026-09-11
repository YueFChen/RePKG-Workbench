using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace RePKG.App.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    public const string EnglishLanguage = "en-US";
    public const string ChineseLanguage = "zh-CN";

    private readonly Dictionary<string, string> _defaults = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, string> _translations = new Dictionary<string, string>();
    private string _currentLanguage = EnglishLanguage;

    public static LocalizationService Current { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage => _currentLanguage;

    public string this[string key] => _translations.TryGetValue(key, out var translated) && !string.IsNullOrWhiteSpace(translated)
        ? translated
        : _defaults.GetValueOrDefault(key, key);

    public void InitializeForCurrentCulture()
    {
        var language = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? ChineseLanguage
            : EnglishLanguage;
        SetLanguage(language);
    }

    public void SetLanguage(string? language)
    {
        var normalized = ResolveLanguage(language);
        var translations = LoadTranslations(normalized);
        if (_currentLanguage == normalized && ReferenceEquals(_translations, translations))
        {
            return;
        }

        _currentLanguage = normalized;
        _translations = translations;
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(normalized);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.GetCultureInfo(EnglishLanguage);
        }
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged("Item[]");
    }

    public string Get(string key, string englishDefault)
    {
        RegisterDefault(key, englishDefault);
        return _translations.TryGetValue(key, out var translated) && !string.IsNullOrWhiteSpace(translated)
            ? translated
            : englishDefault;
    }

    public string Format(string key, string englishDefault, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key, englishDefault), arguments);
    }

    public void RegisterDefault(string key, string englishDefault)
    {
        if (!_defaults.ContainsKey(key))
        {
            _defaults[key] = englishDefault;
        }
    }

    public IReadOnlyList<LanguageDefinition> DiscoverLanguages()
    {
        var languages = new List<LanguageDefinition>();
        LanguageDefinition? english = null;
        var directory = GetLanguageDirectory();
        if (!Directory.Exists(directory))
        {
            return [new(EnglishLanguage, "English")];
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var code = Path.GetFileNameWithoutExtension(path);
            if (!IsSafeLanguageCode(code))
            {
                continue;
            }

            try
            {
                var translations = ReadLanguageFile(path);
                var displayName = translations.GetValueOrDefault("$languageName", GetCultureDisplayName(code));
                var language = new LanguageDefinition(code, displayName);
                if (string.Equals(code, EnglishLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    english = language;
                }
                else
                {
                    languages.Add(language);
                }
            }
            catch (JsonException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
        }

        languages.Insert(0, english ?? new LanguageDefinition(EnglishLanguage, "English"));
        return languages;
    }

    private static IReadOnlyDictionary<string, string> LoadTranslations(string language)
    {
        var path = Path.Combine(GetLanguageDirectory(), $"{language}.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var translations = ReadLanguageFile(path);
            translations.Remove("$languageName");
            return translations;
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
        catch (IOException)
        {
            return new Dictionary<string, string>();
        }
        catch (UnauthorizedAccessException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static string ResolveLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)
            || string.Equals(language, EnglishLanguage, StringComparison.OrdinalIgnoreCase)
            || !IsSafeLanguageCode(language))
        {
            return EnglishLanguage;
        }

        var path = Path.Combine(GetLanguageDirectory(), $"{language}.json");
        return File.Exists(path) ? language : EnglishLanguage;
    }

    private static Dictionary<string, string> ReadLanguageFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }

    private static string GetLanguageDirectory() => Path.Combine(AppContext.BaseDirectory, "Languages");

    private static bool IsSafeLanguageCode(string language)
    {
        return language.Length is > 1 and <= 32
            && language.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private static string GetCultureDisplayName(string code)
    {
        try
        {
            return CultureInfo.GetCultureInfo(code).NativeName;
        }
        catch (CultureNotFoundException)
        {
            return code;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

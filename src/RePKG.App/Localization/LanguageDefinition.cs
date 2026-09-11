namespace RePKG.App.Localization;

public sealed record LanguageDefinition(string Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}

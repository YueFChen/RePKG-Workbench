using RePKG.Core.Scanning;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class ValveKeyValueParserTests
{
    [TestMethod]
    public void Parse_ReadsLibraryPathsAndEscapes()
    {
        const string content = """
            "libraryfolders"
            {
                "0" { "path" "C:\\Program Files (x86)\\Steam" }
                "1" { "path" "D:\\SteamLibrary" }
            }
            """;

        var root = ValveKeyValueParser.Parse(content);
        var libraries = root.Children["libraryfolders"];

        Assert.AreEqual(@"C:\Program Files (x86)\Steam", libraries.Children["0"].GetValue("path"));
        Assert.AreEqual(@"D:\SteamLibrary", libraries.Children["1"].GetValue("path"));
    }

    [TestMethod]
    public void Parse_RejectsUnclosedObjects()
    {
        Assert.ThrowsExactly<FormatException>(() => ValveKeyValueParser.Parse("\"root\" { \"key\" \"value\""));
    }
}

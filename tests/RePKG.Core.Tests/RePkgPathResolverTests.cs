using RePKG.Core.Models;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class RePkgPathResolverTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"repkg-path-{Guid.NewGuid():N}");

    [TestMethod]
    public void Resolve_PreservesExistingConfiguredPath()
    {
        Directory.CreateDirectory(_directory);
        var configuredPath = Path.Combine(_directory, "custom.exe");
        File.WriteAllBytes(configuredPath, []);

        var result = RePkgPathResolver.Resolve(configuredPath, Path.Combine(_directory, "app"));

        Assert.AreEqual(configuredPath, result);
    }

    [TestMethod]
    public void Resolve_ReplacesStaleConfiguredPathWithBundledPath()
    {
        var appDirectory = Path.Combine(_directory, "app");
        var bundledPath = RePkgPathResolver.BundledPath(appDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(bundledPath)!);
        File.WriteAllBytes(bundledPath, []);

        var result = RePkgPathResolver.Resolve(Path.Combine(_directory, "old-build", "RePKG.exe"), appDirectory);

        Assert.AreEqual(bundledPath, result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}

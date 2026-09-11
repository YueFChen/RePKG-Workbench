using System.Text.Json;
using RePKG.Core.Models;
using RePKG.Core.Scanning;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class ProjectScannerTests
{
    [TestMethod]
    public async Task ScanAsync_ParsesAllSupportedProjectTypes()
    {
        using var fixture = new ScannerFixture();
        fixture.AddProject("100", "scene", "Scene", preview: "custom.gif", package: "scene.pkg");
        fixture.AddProject("200", "video", "Video", video: "movie.mp4");
        fixture.AddProject("300", "web", "Web");
        fixture.AddProject("400", "application", "Application");

        var result = await new ProjectScanner().ScanAsync(fixture.RootPath, true);

        Assert.HasCount(4, result.Projects);
        Assert.IsTrue(result.Projects.Any(item => item.Type == ProjectType.Scene && item.PackagePath is not null));
        Assert.IsTrue(result.Projects.Any(item => item.Type == ProjectType.Video && item.VideoPath is not null));
        Assert.IsTrue(result.Projects.Any(item => item.PreviewPath?.EndsWith("custom.gif", StringComparison.Ordinal) == true));
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod]
    public async Task ScanAsync_ReportsBadJsonAndContinues()
    {
        using var fixture = new ScannerFixture();
        fixture.AddProject("good", "web", "Valid project");
        var badDirectory = Directory.CreateDirectory(Path.Combine(fixture.RootPath, "bad")).FullName;
        await File.WriteAllTextAsync(Path.Combine(badDirectory, "project.json"), "{ invalid");

        var result = await new ProjectScanner().ScanAsync(fixture.RootPath, true);

        Assert.HasCount(1, result.Projects);
        Assert.HasCount(1, result.Warnings);
        StringAssert.Contains(result.Warnings[0].Path, "project.json");
    }

    private sealed class ScannerFixture : IDisposable
    {
        public ScannerFixture()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"repkg-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void AddProject(
            string directoryName,
            string type,
            string title,
            string? preview = null,
            string? package = null,
            string? video = null)
        {
            var directory = Directory.CreateDirectory(Path.Combine(RootPath, directoryName)).FullName;
            var project = new Dictionary<string, object?>
            {
                ["type"] = type,
                ["title"] = title,
                ["preview"] = preview,
                ["file"] = video,
                ["workshopid"] = directoryName,
            };
            File.WriteAllText(Path.Combine(directory, "project.json"), JsonSerializer.Serialize(project));

            foreach (var file in new[] { preview, package, video }.Where(file => file is not null))
            {
                File.WriteAllText(Path.Combine(directory, file!), "fixture");
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, true);
            }
        }
    }
}

using RePKG.Core.Extraction;
using RePKG.Core.Models;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class DirectoryProjectCopierTests
{
    [TestMethod]
    public async Task CopyAsync_CopiesCompleteProjectTree()
    {
        var root = Path.Combine(Path.GetTempPath(), $"repkg-copy-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "input", "123");
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(Path.Combine(source, "assets"));
        await File.WriteAllTextAsync(Path.Combine(source, "project.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(source, "assets", "content.txt"), "data");

        try
        {
            var project = new ProjectItem(source, null, "fixture", ProjectType.Web, null, 0, "123");

            var result = await new DirectoryProjectCopier().CopyAsync(project, output, false);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(File.Exists(Path.Combine(output, "123", "project.json")));
            Assert.IsTrue(File.Exists(Path.Combine(output, "123", "assets", "content.txt")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}

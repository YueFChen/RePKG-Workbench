using RePKG.Core.Models;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class PathSafetyTests
{
    [TestMethod]
    public void GetContainedPath_ReturnsNormalizedChild()
    {
        var root = Path.Combine(Path.GetTempPath(), "repkg-output");

        var result = PathSafety.GetContainedPath(root, "12345");

        Assert.AreEqual(Path.Combine(root, "12345"), result);
    }

    [TestMethod]
    public void GetContainedPath_RejectsTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "repkg-output");

        Assert.ThrowsExactly<InvalidOperationException>(() => PathSafety.GetContainedPath(root, ".."));
    }
}

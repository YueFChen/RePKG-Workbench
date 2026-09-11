using RePKG.Core.Extraction;

namespace RePKG.Core.Tests;

[TestClass]
public sealed class ExtractionProgressTests
{
    [TestMethod]
    public void Percentage_UsesCompletedItemsOnly()
    {
        var progress = new ExtractionProgress(2, 4, "current-project", null);

        Assert.AreEqual(50d, progress.Percentage);
    }

    [TestMethod]
    public void Percentage_IsZeroWhenThereAreNoItems()
    {
        var progress = new ExtractionProgress(0, 0, null, null);

        Assert.AreEqual(0d, progress.Percentage);
    }
}
